﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JPB.DataAccess.AdoWrapper;
using JPB.DataAccess.DbEventArgs;
using JPB.DataAccess.DebuggerHelper;

namespace JPB.DataAccess.Manager
{
    /// <summary>
    /// A database operation is done
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DatabaseActionHandler(object sender, DatabaseActionEvent e);

    /// <summary>
    /// A database operation is done
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void DatabaseFailedActionHandler(object sender, Exception e);

    partial class DbAccessLayer
    {
        /// <summary>
        /// Should raise Instance bound Events
        /// </summary>
        [Obsolete]
        public bool RaiseEvents { get; set; }

        /// <summary>
        /// Should raise non Instance bound Events
        /// </summary>
        public static bool RaiseStaticEvents { get; set; }

        public static event DatabaseActionHandler OnDelete;
        public static event DatabaseActionHandler OnSelect;
        public static event DatabaseActionHandler OnUpdate;
        public static event DatabaseActionHandler OnInsert;
        public static event DatabaseFailedActionHandler OnException;

        protected internal static void RaiseDelete(object sender, IDbCommand query, IDatabase source)
        {
            if (!RaiseStaticEvents)
                return;

            var handler = OnDelete;
            if (handler != null)
                handler.BeginInvoke(sender, new DatabaseActionEvent(query.CreateQueryDebugger()), s => { }, null);
        }

        protected internal static void RaiseException(object sender, Exception ex)
        {
            if (!RaiseStaticEvents)
                return;

            var handler = OnException;
            if (handler != null)
                handler.BeginInvoke(sender, ex, s => { }, null);
        }

        protected internal static void RaiseSelect(IDbCommand query, IDatabase source)
        {
            if (!RaiseStaticEvents)
                return;
            var handler = OnSelect;
            if (handler != null)
                handler.BeginInvoke(null, new DatabaseActionEvent(query.CreateQueryDebugger()), s => { }, null);
        }

        protected internal static void RaiseUpdate(object sender, IDbCommand query, IDatabase source)
        {
            if (!RaiseStaticEvents)
                return;

            var handler = OnUpdate;
            if (handler != null)
                handler.BeginInvoke(sender, new DatabaseActionEvent(query.CreateQueryDebugger()), s => { }, null);
        }

        protected internal static void RaiseInsert(object sender, IDbCommand query, IDatabase source)
        {
            if (!RaiseStaticEvents)
                return;

            var handler = OnInsert;
            if (handler != null)
                handler.BeginInvoke(sender, new DatabaseActionEvent(query.CreateQueryDebugger()), s => { }, null);
        }
    }
}
