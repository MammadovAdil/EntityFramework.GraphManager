using Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers.Abstract;
using Ma.EntityFramework.GraphManager.Models;
using System;
using System.Collections.Generic;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager.Helpers
{
    public class HelperStore
    {
        public Dictionary<Type, object> EntityManager { get; private set; }
        public Dictionary<string, IGraphEntityTypeManager> EntityTypeManager { get; private set; }
        public Dictionary<object, object> UppermostParent { get; private set; }
        public Dictionary<object, object> UppermostPrincipalParent { get; private set; }  
        public Dictionary<string, NavigationDetail> NavigationDetail { get; private set; }      
        public Dictionary<Tuple<string, string>, string> ForeignKeyOrigin { get; private set; }

        public HelperStore()
        {
            EntityManager = new Dictionary<Type, object>();
            EntityTypeManager = new Dictionary<string, IGraphEntityTypeManager>();
            UppermostParent = new Dictionary<object, object>();
            UppermostPrincipalParent = new Dictionary<object, object>();
            NavigationDetail = new Dictionary<string, Models.NavigationDetail>();
            ForeignKeyOrigin = new Dictionary<Tuple<string, string>, string>();            
        }
    }
}
