using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Typewriter.CodeModel.Attributes;

namespace Typewriter.CodeModel
{
    /// <summary>
    /// Represents a all project files.
    /// </summary>
    [Context("RootContext", "RootContexets")]
    public abstract class RootContext : Item
    {
        /// <summary>
        /// All public files.
        /// </summary>
        public abstract FilesCollection Files { get; }

        /// <summary>
        /// All public classes.
        /// </summary>
        public abstract ClassCollection Classes { get; }

        /// <summary>
        /// All public delegates.
        /// </summary>
        public abstract DelegateCollection Delegates { get; }

        /// <summary>
        /// All public enums.
        /// </summary>
        public abstract EnumCollection Enums { get; }

        /// <summary>
        /// All public interfaces.
        /// </summary>
        public abstract InterfaceCollection Interfaces { get; }
    }
}
