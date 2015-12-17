﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace JPB.DataAccess.Config.Model
{
    public class ConstructorInfoCache
    {
        public ConstructorInfoCache(ConstructorInfo ctorInfo)
        {
            this.AttributeInfoCaches = new List<AttributeInfoCache>();
            if (ctorInfo != null)
            {
                MethodInfo = ctorInfo;
                MethodName = ctorInfo.Name;
                this.AttributeInfoCaches =
                    ctorInfo
                    .GetCustomAttributes(true)
                    .Where(s => s is Attribute)
                    .Select(s => new AttributeInfoCache(s as Attribute))
                    .ToList();
            }
        }
        
        public ConstructorInfo MethodInfo { get; private set; }
        public string MethodName { get; private set; }
        public List<AttributeInfoCache> AttributeInfoCaches { get; private set; }
    }
}
