//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace JPB.DataAccess.EntryCreator.AutoGeneratedEntrys {
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Data;
    using JPB.DataAccess.ModelsAnotations;
    
    
    public class LoadXmlTest {
        
        [ObjectFactoryMethodAttribute()]
        public LoadXmlTest(IDataRecord record) {
            ID_test = ((long)(record["ID_test"]));
            PropA = ((string)(record["PropA"]));
            PropB = ((string)(record["PropB"]));
        }
        
        private long _iD_test;
        
        private string _propA;
        
        private string _propB;
        
        [PrimaryKeyAttribute()]
        public long ID_test {
            get {
                return this._iD_test;
            }
            set {
                this._iD_test = value;
            }
        }
        
        public string PropA {
            get {
                return this._propA;
            }
            set {
                this._propA = value;
            }
        }
        
        public string PropB {
            get {
                return this._propB;
            }
            set {
                this._propB = value;
            }
        }
    }
}
