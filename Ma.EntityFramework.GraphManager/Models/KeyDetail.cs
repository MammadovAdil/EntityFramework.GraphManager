using System.Collections.Generic;

namespace Ma.EntityFramework.GraphManager.Models
{
    /// <summary>
    /// Name of conatiner class with the list of names of keys
    /// </summary>
    public class KeyDetail
    {
        internal KeyDetail()
        {
            Keys = new List<string>();
        }

        internal string ContainerClass { get; set; }
        internal List<string> Keys { get; set; }
    }
}
