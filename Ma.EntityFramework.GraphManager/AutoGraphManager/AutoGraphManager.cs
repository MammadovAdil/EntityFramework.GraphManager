using Ma.EntityFramework.GraphManager.AutoGraphManager.Abstract;
using System;
using System.Data.Entity;

namespace Ma.EntityFramework.GraphManager.AutoGraphManager
{
    public class AutoGraphManager
        : IAutoGraphManager
    {
        internal DbContext Context { get; set; }

        public AutoGraphManager(DbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            Context = context;
        }
    }
}
