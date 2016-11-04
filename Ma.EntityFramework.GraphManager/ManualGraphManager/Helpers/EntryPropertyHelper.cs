using Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers.Abstract;
using System;
using System.Data.Entity.Infrastructure;

namespace Ma.EntityFramework.GraphManager.ManualGraphManager.Helpers
{
    internal class EntryPropertyHelper<TProperty>
        : IEntryPropertyHelper<TProperty>
    {
        private DbPropertyEntry EntryProperty { get; set; }

        /// <summary>
        /// EntryProperty helper to work on property of entry.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// When entryPropertyParam is null.
        /// </exception>
        /// <param name="entryPropertyParam">Entry proeprty to work on.</param>
        public EntryPropertyHelper(DbPropertyEntry entryPropertyParam)
        {
            if (entryPropertyParam == null)
                throw new ArgumentNullException("entryPropertyParam");

            EntryProperty = entryPropertyParam;            
        }

        /// <summary>
        /// Get or set if property is modified.
        /// </summary>
        public bool IsModified
        {
            get { return EntryProperty.IsModified; }
            set { EntryProperty.IsModified = value; }
        }

        /// <summary>
        /// Current value of property entry.
        /// </summary>
        public TProperty Value
        {
            get { return (TProperty)EntryProperty.CurrentValue; }
        }
    }
}
