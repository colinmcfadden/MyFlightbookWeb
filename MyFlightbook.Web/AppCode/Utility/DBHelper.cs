﻿using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;

/******************************************************
 * 
 * Copyright (c) 2008-2023 MyFlightbook LLC
 * Contact myflightbook-at-gmail.com for more information
 *
*******************************************************/

namespace MyFlightbook
{
    /// <summary>
    /// Represents a query, including the querystring and any parameters.  This can be passed to DBHeloper instead of a MySQLCommandObject so that DBHelper can stop exposing the command object (which implements IDisposable)
    /// </summary>
    public class DBHelperCommandArgs
    {
        #region Properties
        /// <summary>
        /// Parameters for the query
        /// </summary>
        public Collection<MySqlParameter> Parameters { get; private set; }

        /// <summary>
        /// The query string itself
        /// </summary>
        public string QueryString { get; set; }

        /// <summary>
        /// Timeout to use; 0 for the default.
        /// </summary>
        public int Timeout { get; set; }
        #endregion

        #region Helper methods
        public void AddWithValue(string paramname, object o)
        {
            Parameters.Add(new MySqlParameter(paramname, o));
        }

        /// <summary>
        /// Copies the parameters from an enumerable into in bulk.
        /// </summary>
        /// <param name="rgIn">Enumerable of aprameters</param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddFrom(IEnumerable<MySqlParameter> rgIn)
        {
            if (rgIn == null)
                throw new ArgumentNullException(nameof(rgIn));

            foreach (MySqlParameter param in rgIn)
                Parameters.Add(param);
        }
        #endregion

        #region Constructors
        public DBHelperCommandArgs()
        {
            Parameters = new Collection<MySqlParameter>();
            QueryString = string.Empty;
            Timeout = 0;
        }

        public DBHelperCommandArgs(string szQ) : this()
        {
            QueryString = szQ;
        }

        public DBHelperCommandArgs(string szQ, IEnumerable<MySqlParameter> pcoll) : this()
        {
            QueryString = szQ;
            if (pcoll != null)
                foreach (MySqlParameter p in pcoll)
                    Parameters.Add(p);
        }
        #endregion
    }

    /// <summary>
    /// Utility class for working with the database.  Abstracts out working with the connection object.
    /// </summary>
    public class DBHelper
    {
        #region properties
        /// <summary>
        /// The exception or other error from the last operation.
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// ID of the most recently inserted row.
        /// </summary>
        public Int32 LastInsertedRowId { get; set; }

        /// <summary>
        /// Number of rows affected by the most recent nonquery (update/delete/replace).  -1 if there is none.
        /// </summary>
        public int AffectedRowCount { get; set; }

        /// <summary>
        /// Arguments for setting up a command.
        /// </summary>
        public DBHelperCommandArgs CommandArgs { get; set; }

        private readonly CommandType CommandType = CommandType.Text;

        /// <summary>
        /// The SQL query string.
        /// </summary>
        public string CommandText
        {
            get { return CommandArgs.QueryString; }
            set { CommandArgs.QueryString = value; }
        }
        #endregion

        #region Constructors
        public DBHelper() {
            CommandArgs = new DBHelperCommandArgs();
            AffectedRowCount = -1;
        }

        public DBHelper(string szQuery, CommandType commandType = CommandType.Text) : this()
        {
            CommandText = szQuery;
            CommandType = commandType;
        }

        public DBHelper(DBHelperCommandArgs args, CommandType commandType = CommandType.Text) : this()
        {
            CommandArgs = args ?? throw new ArgumentNullException(nameof(args));
            CommandType = commandType;
        }
        #endregion

        public enum ReadRowMode { AllRows, SingleRow };

        #region Command initialization
        public static string ConnectionString { get { return ConfigurationManager.ConnectionStrings["logbookConnectionString"].ConnectionString; } }
        /// <summary>
        /// Returns a command object for the query string, to which parameters can be attached.
        /// THE CONNECTION IS NOT INITIALIZED - IT IS UP TO THE CALLER TO using(MySqlConnection = new ....) { } IT!!!
        /// </summary>
        /// <param name="args">The specification of the query string and any pre-initialized parameters</param>
        /// <returns>A usable MySqlCommand object</returns>
        public static void InitCommandObject(MySqlCommand comm, DBHelperCommandArgs args, CommandType commandType = CommandType.Text)
        {
            if (comm == null)
                throw new ArgumentNullException(nameof(comm));
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            comm.CommandText = args.QueryString;
            comm.Parameters.AddRange(args.Parameters.ToArray());
            if (args.Timeout > 0)
                comm.CommandTimeout = args.Timeout;
            comm.CommandType = commandType;
        }
        #endregion

        #region Reading data
        /// <summary>
        /// Where you want to re-use an args object, this can be an easier method than using a new DBHelper
        /// </summary>
        /// <param name="args"></param>
        /// <param name="szQ"></param>
        /// <param name="onRead"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void ExecuteWithArgs(DBHelperCommandArgs args, string szQ, Action<MySqlDataReader> onRead)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (szQ == null)
                throw new ArgumentNullException(nameof(szQ));
            using (MySqlCommand comm = new MySqlCommand())
            {
                InitCommandObject(comm, args);
                using (comm.Connection = new MySqlConnection(ConnectionString))
                {
                    MySqlDataReader dr = null;
                    comm.CommandText = szQ;
                    comm.Connection.Open();

                    using (dr = comm.ExecuteReader())
                    {
                        onRead?.Invoke(dr);
                    }
                }
            }
        }

        /// <summary>
        /// Executes a query
        /// </summary>
        /// <param name="args">Query string plus any pre-initialized parameters</param>
        /// <param name="initCommand">Lambda to add any necessary parameters or otherwise pre-process the command</param>
        /// <param name="readRow">Lambda to read a row.  All rows will be read unless true is returned</param>
        /// <param name="rowMode">Row mode - read all available rows or just a single row</param>
        /// <returns>True for success.  Sets LastError</returns>
        public bool ReadRows(DBHelperCommandArgs args, Action<MySqlCommand> initCommand, Action<MySqlDataReader> readRow, ReadRowMode rowMode)
        {
            if (args == null)
                throw new ArgumentNullException(nameof(args));
            if (readRow == null)
                throw new ArgumentNullException(nameof(readRow));

            bool fResult = true;
            using (MySqlCommand comm = new MySqlCommand())
            {
                InitCommandObject(comm, args, CommandType);

                using (comm.Connection = new MySqlConnection(ConnectionString))
                {
                    LastError = string.Empty;

                    initCommand?.Invoke(comm);
                    try
                    {
                        comm.Connection.Open();
                        using (MySqlDataReader dr = comm.ExecuteReader())
                        {
                            if (dr.HasRows)
                            {
                                while (dr.Read())
                                {
                                    readRow(dr);
                                    if (rowMode == ReadRowMode.SingleRow)
                                        break;
                                }
                            }
                        }
                    }
                    catch (MySqlException ex)
                    {
                        throw new MyFlightbookException("MySQL error thrown in ReadRows; Query = " + CommandText ?? string.Empty, ex, string.Empty);
                    }
                    catch (MyFlightbookException ex)
                    {
                        LastError = ex.Message;
                        fResult = false;
                    }
                    catch (Exception ex)
                    {
                        MyFlightbookException exNew = new MyFlightbookException(String.Format(CultureInfo.CurrentCulture, "Uncaught exception in ReadRows:\r\n:{0}", comm.CommandText), ex);
                        MyFlightbookException.NotifyAdminException(exNew);
                        throw exNew;
                    }
                }
            }
            return fResult;
        }

        public bool ReadRows(string szQ, Action<MySqlCommand> initCommand, Action<MySqlDataReader> readRow)
        {
            DBHelperCommandArgs args = new DBHelperCommandArgs() { QueryString = szQ };
            return ReadRows(args, initCommand, readRow, ReadRowMode.AllRows);
        }

        public bool ReadRows(string szQ, Action<MySqlCommand> initCommand, Action<MySqlDataReader> readRow, ReadRowMode rowMode)
        {
            DBHelperCommandArgs args = new DBHelperCommandArgs() { QueryString = szQ };
            return ReadRows(args, initCommand, readRow, rowMode);
        }

        /// <summary>
        /// Executes a query using the already-established command text, reading all rows
        /// </summary>
        /// <param name="initCommand">Lambda to add any necessary parameters or otherwise pre-process the command</param>
        /// <param name="readRow">Lambda to read a row.  All rows will be read unless true is returned</param>
        /// <returns>True for success.  Sets LastError</returns>
        public bool ReadRows(Action<MySqlCommand> initCommand, Action<MySqlDataReader> readRow)
        {
            return ReadRows(CommandArgs, initCommand, readRow, ReadRowMode.AllRows);
        }

        /// <summary>
        /// Executes a query using the already-established command text, reading a single row
        /// </summary>
        /// <param name="initCommand">Lambda to add any necessary parameters or otherwise pre-process the command</param>
        /// <param name="readRow">Lambda to read a row.  All rows will be read unless true is returned</param>
        /// <returns>True for success.  Sets LastError</returns>
        public bool ReadRow(Action<MySqlCommand> initCommand, Action<MySqlDataReader> readRowAction)
        {
            return ReadRows(CommandArgs, initCommand, readRowAction, ReadRowMode.SingleRow);
        }
        #endregion

        #region Scalar (non-query) utilities)
        /// <summary>
        /// Execute a query that does not have rows as a result (e.g., an insert or a delete rather than a select).
        /// </summary>
        /// <param name="args">The DBHelpercommandArgs object to use; if not provided, the CommandArgs property is used.</param>
        /// <param name="initCommand">A lambdat to add any necessary parameters or otherwise pre-process the command</param>
        /// <returns>True for success</returns>
        public bool DoNonQuery(DBHelperCommandArgs args, Action<MySqlCommand> initCommand = null)
        {
            if (args == null)
                args = CommandArgs;

            bool fResult = true;
            using (MySqlCommand comm = new MySqlCommand())
            {
                InitCommandObject(comm, args, CommandType);

                initCommand?.Invoke(comm);

                using (comm.Connection = new MySqlConnection(ConnectionString))
                {
                    LastError = string.Empty;
                    try
                    {
                        comm.Connection.Open();
                        AffectedRowCount = comm.ExecuteNonQuery();
                        comm.CommandText = "SELECT Last_Insert_Id()";
                        LastInsertedRowId = Convert.ToInt32(comm.ExecuteScalar(), CultureInfo.InvariantCulture);
                    }
                    catch (MySqlException ex)
                    {
                        LastError = ex.Message;
                        fResult = false;
                        throw new MyFlightbookException("Exception DoNonQuery:\r\nCode = " + ex.ErrorCode.ToString(CultureInfo.InvariantCulture) + "\r\n" + ex.Message + "\r\n" + comm.CommandText, ex);
                    }
                }
            }
            return fResult;
        }

        /// <summary>
        /// Execute a query that does not have rows as a result (e.g., an insert or a delete rather than a select).
        /// </summary>
        /// <param name="initCommand">A lambdat to add any necessary parameters or otherwise pre-process the command</param>
        /// <param name="szQ">The query string</param>
        /// <returns></returns>
        public bool DoNonQuery(string szQ, Action<MySqlCommand> initCommand = null)
        {
            return DoNonQuery(new DBHelperCommandArgs(szQ), initCommand);
        }

        /// <summary>
        /// Execute a query that does not have rows as a result (e.g., an insert or a delete rather than a select).  Uses the existing CommandArgs property
        /// </summary>
        /// <param name="initCommand">A lambdat to add any necessary parameters or otherwise pre-process the command</param>
        /// <returns></returns>
        public bool DoNonQuery(Action<MySqlCommand> initCommand = null)
        {
            return DoNonQuery(CommandArgs, initCommand);
        }
        #endregion

        private static string dbVer = null;

        /// <summary>
        /// Return the database version as a string; do with it what you will...
        /// </summary>
        /// <returns></returns>
        public static string GetDbVer()
        {
            if (dbVer == null)
            {
                DBHelper dbh = new DBHelper("SHOW VARIABLES LIKE 'version'");
                dbh.ReadRow((comm) => { }, (dr) => { dbVer = dr["Value"].ToString(); });
            }
            return dbVer;
        }
    }
}