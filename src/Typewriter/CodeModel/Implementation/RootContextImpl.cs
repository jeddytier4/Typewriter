using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Typewriter.CodeModel.Collections;
using Typewriter.Metadata.Interfaces;

namespace Typewriter.CodeModel.Implementation
{
    public class RootContextImpl : RootContext
    {
        private readonly IEnumerable<IFileMetadata> _metadata;

        public RootContextImpl(IEnumerable<IFileMetadata> metadata)
        {
            _metadata = metadata;
        }

        private FilesCollection _files;
        private ClassCollectionImpl _classes;
        private DelegateCollection _delegates;
        private EnumCollection _enums;
        private InterfaceCollection _interfaces;

        public override FilesCollection Files => _files ?? (_files = new FilesCollectionImpl(_metadata.Select(m => new FileImpl(m))));

        public override ClassCollection Classes => _classes ?? (_classes = new ClassCollectionImpl(Files.SelectMany(f => f.Classes)));

        public override DelegateCollection Delegates => _delegates ?? (_delegates = new DelegateCollectionImpl(Files.SelectMany(f => f.Delegates)));

        public override EnumCollection Enums => _enums ?? (_enums = new EnumCollectionImpl(Files.SelectMany(f => f.Enums)));

        public override InterfaceCollection Interfaces => _interfaces ?? (_interfaces = new InterfaceCollectionImpl(Files.SelectMany(f => f.Interfaces)));
    }
}
