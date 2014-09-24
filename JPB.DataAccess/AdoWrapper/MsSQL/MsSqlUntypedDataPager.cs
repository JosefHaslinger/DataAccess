﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using JPB.DataAccess.Manager;
using JPB.DataAccess.Pager.Contracts;

namespace JPB.DataAccess.AdoWrapper.MsSql
{
    public class MsSqlUntypedDataPager : IUnGenericDataPager
    {
        public MsSqlUntypedDataPager()
        {
            CurrentPage = 0;
            PageSize = 10;

            CurrentPageItems = new ObservableCollection<dynamic>();
            FirstID = -1;
            LastID = -1;
            SyncHelper = action => action();
        }

        public bool Cache
        {
            get { return _cache; }
            set
            {
                if (value)
                    throw new Exception("To be supported ... sory");
                _cache = value;
            }
        }

        public long FirstID { get; private set; }
        public long LastID { get; private set; }

        public long CurrentPage
        {
            get { return _currentPage; }
            set
            {
                if (value >= 0)
                    _currentPage = value;
            }
        }

        public long MaxPage { get; private set; }

        private string SqlVersion;
        
        public int PageSize { get; set; }

        public ICollection<dynamic> CurrentPageItems { get; private set; }
        public Type TargetType { get; set; }

        public void LoadPage(DbAccessLayer dbAccess)
        {
            if (string.IsNullOrEmpty(SqlVersion))
            {
                SqlVersion = dbAccess.RunPrimetivSelect<string>("SELECT SERVERPROPERTY('productversion')").FirstOrDefault();
            }

            SyncHelper(CurrentPageItems.Clear);

            var pk = TargetType.GetPK();

            if (FirstID == -1 || LastID == -1)
            {
                var firstOrDefault = dbAccess.RunPrimetivSelect(typeof(long), "SELECT TOP 1 " + pk + " FROM " + TargetType.GetTableName() + " ORDER BY " + pk).FirstOrDefault();
                if (firstOrDefault != null)
                    FirstID = (long)firstOrDefault;

                var lastId = dbAccess.RunPrimetivSelect(typeof(long), "SELECT TOP 1 " + pk + " FROM " + TargetType.GetTableName() + " ORDER BY " + pk + " DESC").FirstOrDefault();
                if (lastId != null)
                    LastID = (long)lastId;
            }

            var maxItems = dbAccess.RunPrimetivSelect(typeof(long), "SELECT COUNT( * ) AS NR FROM " + TargetType.GetTableName()).FirstOrDefault();
            if (maxItems != null)
            {
                long parsedCount;
                long.TryParse(maxItems.ToString(), out parsedCount);
                MaxPage = ((long)parsedCount) / PageSize;
            }

            //Check select strategy
            //IF version is or higher then 11.0.2100.60 we can use OFFSET and FETCH
            //esle we need to do it the old way
            List<dynamic> selectWhere = null;
            if (CheckVersionForFetch())
            {
                selectWhere = dbAccess.SelectWhere(TargetType," ORDER BY " + pk + " ASC OFFSET @PagedRows ROWS FETCH NEXT @PageSize ROWS ONLY", new
                {
                    PagedRows = CurrentPage * PageSize,
                    PageSize
                });
            }
            else
            {
                var queryBuilde = new StringBuilder();
                queryBuilde.Append("SELECT * ");
                queryBuilde.Append(" FROM (");
                queryBuilde.Append("SELECT ROW_NUMBER() OVER (ORDER BY ");
                queryBuilde.Append(pk);
                queryBuilde.Append(") AS NUMBER, *");
                queryBuilde.Append(" FROM ");
                queryBuilde.Append(TargetType.GetTableName());
                queryBuilde.Append(") AS TBL ");
                queryBuilde.Append("WHERE NUMBER BETWEEN ((@PagedRows - 1) * @PageSize + 1) AND (@PagedRows * @PageSize)");
                queryBuilde.Append("ORDER BY ");
                queryBuilde.Append(pk);

                selectWhere = dbAccess.SelectNative(TargetType, queryBuilde.ToString(), new
                {
                    PagedRows = CurrentPage + 1,
                    PageSize
                });
            }

            foreach (var item in selectWhere)
            {
                dynamic item1 = item;
                SyncHelper(() => CurrentPageItems.Add(item1));
            }
        }

        private bool? _checkRun;
        private Action<Action> _syncHelper;
        private long _currentPage;
        private bool _cache;

        private bool CheckVersionForFetch()
        {
            if (_checkRun != null)
                return _checkRun.Value;


            var versionParts = SqlVersion.Split('.');

            //Target 11.0.2100.60 or higher
            //      Major Version
            //      Minor Version 
            //      Build Number
            //      Revision

            var major = int.Parse(versionParts[0]);
            var minor = int.Parse(versionParts[1]);
            var build = int.Parse(versionParts[2]);
            var revision = int.Parse(versionParts[3]);

            if (major > 11)
            {
                _checkRun = true;
            }
            else if (major == 11)
            {
                if (minor > 0)
                {
                    _checkRun = true;
                }
                else if (minor == 0)
                {
                    if (build > 2100)
                    {
                        _checkRun = true;
                    }
                    else if (build == 2100)
                    {
                        if (revision >= 60)
                        {
                            _checkRun = true;
                        }
                    }
                }
            }
            else
            {
                _checkRun = false;
            }

            return _checkRun != null && _checkRun.Value;
        }

        public Action<Action> SyncHelper
        {
            get { return _syncHelper; }
            set
            {
                if (value != null)
                    _syncHelper = value;
            }
        }
    }
}