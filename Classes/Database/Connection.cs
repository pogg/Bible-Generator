﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using BibleProject.Classes.Text;
using MySql.Data.MySqlClient;
using System.Windows.Forms;
using BibleProject.Classes.Database.Queries;
using System.IO;

namespace BibleProject.Classes.Database
{
    internal class DatabaseConnection
    {

        private static int MySqlTableLength { get; set; }
        private static int SqlServerTableLength { get; set; }

        static Forms.frm_MainWindow mw;

        // MySQL
        public static void Open(Forms.frm_MainWindow _mw, string server, string database, string username, string password, QueryLanguage ql)
        {
            mw = _mw;
            using (MySqlConnection con = new MySqlConnection("Server=" + server + ";Database=" + database + ";Uid=" + username + ";Pwd=" + password + "; CharSet=utf8;"))
            {
                // Checks if table exists or not. If it doesn't, create it. If not, continue.
                CheckIfMySqlTableExists(con, database, ql);

                MySqlTableLength = 0;
                // Inserts data into database
                InsertIntoMySqlDatabase(con, ql);
            }
        }

        // SQL Server
        public static void Open(Forms.frm_MainWindow _mw, string connectionString, QueryLanguage ql)
        {
            mw = _mw;
            using (SqlConnection con = new SqlConnection(connectionString))
            {
                // Checks if table exists or not. If it doesn't, create it. If not, continue.
                CheckIfSqlServerTableExists(con, ql);

                SqlServerTableLength = 0;

                // Inserts data into database
                InsertIntoSqlServerDatabase(con, ql);
            }
        }


        // MySql
        private static void CheckIfMySqlTableExists(MySqlConnection con, string dbName, QueryLanguage ql)
        {
            try
            {
                con.Open();
            }
            catch (MySqlException mse)
            {
                MessageBox.Show("Unable to open connection to MySQL Database. Please recheck your credentials and try again:\n\n" + mse, "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mw.ResetButtons();
            }

            using (MySqlCommand cmd = new MySqlCommand(Queries.MySql.CheckIfTableExists(ql)))
            {
                cmd.Connection = con;

                MySqlDataReader r = cmd.ExecuteReader();
                while (r.Read())
                {
                    if (r.GetString(0).Length > 0)
                    {
                        MySqlTableLength = r.GetString(0).Length;
                    }
                }

                r.Close();

                if (MySqlTableLength == 0)
                {
                    cmd.CommandText = Queries.MySql.GetTableCreationString(ql);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (MySqlException mse)
                    {
                        MessageBox.Show("Error when checking if table exists in MySQL Database: " + Environment.NewLine + Environment.NewLine + mse, "Unable to check table", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        mw.ResetButtons();
                    }
                }
            }
            con.Close();

        }

        // Sql Server
        private static void CheckIfSqlServerTableExists(SqlConnection con, QueryLanguage ql)
        {
            try
            {
                con.Open();
            }
            catch (SqlException)
            {
                MessageBox.Show("Unable to open connection to SQL Server Database. Please recheck your Connection String and try again.", "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mw.ResetButtons();
            }

            string Query = Queries.SqlServer.CheckIfTableExists(ql);
            using (SqlCommand cmd = new SqlCommand(Query))
            {
                try
                {
                    cmd.Connection = con;
                }
                catch (SqlException)
                {
                    MessageBox.Show("Unable to open connection to SQL Server Database. Please recheck your Connection String and try again.", "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    mw.ResetButtons();
                }
                SqlDataReader r = cmd.ExecuteReader();

                while (r.Read())
                {
                    if (r.GetString(0).Length > 0)
                    {
                        SqlServerTableLength = r.GetString(0).Length;
                    }
                }

                r.Close();

                if (SqlServerTableLength == 0)
                {
                    cmd.CommandText = Queries.SqlServer.GetTableCreationString(ql);
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException se)
                    {
                        MessageBox.Show("Error when checking if table exists in SQL Server Database: " + Environment.NewLine + Environment.NewLine + se, "Unable to check table", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                        mw.ResetButtons();
                    }
                }
            }
            con.Close();
        }

        // MySql
        private static void InsertIntoMySqlDatabase(MySqlConnection con, QueryLanguage ql)
        {
            try
            {
                con.Open();
            }
            catch (MySqlException)
            {
                MessageBox.Show("Unable to open connection to MySQL Database while attempting to insert data. Please recheck your credentials and try again", "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mw.ResetButtons();
            }


            List<BibleCollection> LanguageCollection = new List<BibleCollection>();
            LanguageCollection = MemoryStorage.FullDataCollection[(int)ql].BibleCollection;

            StringBuilder sb = new StringBuilder();

            bool firstStringFinished = false;

            foreach (var fc in LanguageCollection)
            {
                if (!firstStringFinished)
                {
                    sb.Append("INSERT INTO " + ql.ToString() + "(Book, Chapter, Verse, Word) " + Environment.NewLine + " VALUES('" + fc.CurrentBook + "', " + fc.Chapter + ", " + fc.Verse + ", \"" + fc.Word + "\")" + Environment.NewLine);
                    firstStringFinished = true;
                }
                else
                {
                    sb.Append(", ('" + fc.CurrentBook + "', " + fc.Chapter + ", " + fc.Verse + ", \"" + fc.Word + "\")" + Environment.NewLine);
                }
                MemoryStorage.CurrentQuery++;
                mw.UpdateQueryProgress();
            }
            sb.Append(";" + Environment.NewLine + Environment.NewLine);

            using (StreamWriter w = new StreamWriter("debug-me.sql", true))
            {
                w.Write(sb.ToString());
            }

            using (MySqlCommand cmd = new MySqlCommand(sb.ToString()))
            {
                cmd.Connection = con;
                cmd.CommandTimeout = 999999;
                try
                {
                    cmd.ExecuteNonQueryAsync();
                }
                catch (MySqlException mse)
                {
                    MessageBox.Show("Unable to insert data into MySQL database:" + Environment.NewLine + Environment.NewLine + mse, "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    mw.ResetButtons();
                }
            }

            con.Close();
        }

        // Sql Server. Extremely slow. Switch to StringBuilder or Bulk Insert.
        private static void InsertIntoSqlServerDatabase(SqlConnection con, QueryLanguage ql)
        {
            try
            {
                con.Open();
            }
            catch (SqlException se)
            {
                MessageBox.Show("Unable to open connection to SQL Server database while attempting to insert data:" + Environment.NewLine + Environment.NewLine + se, "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                mw.ResetButtons();
            }

            List<BibleCollection> LanguageCollection = new List<BibleCollection>();
            LanguageCollection = MemoryStorage.FullDataCollection[(int)ql].BibleCollection;

            StringBuilder sb = new StringBuilder();


            int currentQuery = 0;
            
            foreach (var fc in LanguageCollection)
            {
                sb.Append("INSERT INTO " + ql.ToString() + "(Book, Chapter, Verse, Word) VALUES(N'" + fc.CurrentBook + "', " + fc.Chapter + ", " + fc.Verse + ", N'" + fc.Word.Replace("'", "''") + "');" + Environment.NewLine);

                currentQuery++;
                MemoryStorage.CurrentQuery++;
                mw.UpdateQueryProgress();
            }

            sb.Append(";" + Environment.NewLine + Environment.NewLine);

                using (SqlCommand cmd = new SqlCommand(sb.ToString(), con))
                {
                    try
                    {
                        cmd.ExecuteNonQuery();
                    }
                    catch (SqlException se)
                    {
                        MessageBox.Show("Unable to insert data into SQL Server Database:" + Environment.NewLine + Environment.NewLine + se, "Potential User Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        mw.ResetButtons();
                    }
                    MemoryStorage.CurrentQuery++;
                    mw.UpdateQueryProgress();
                }
            con.Close();
        }
    }
}