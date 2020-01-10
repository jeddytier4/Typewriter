using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Typewriter.CodeModel.Collections
{
    public class FilesCollectionImpl : ItemCollectionImpl<File>, FilesCollection
    {
        public FilesCollectionImpl(IEnumerable<File> values) : base(values)
        {

        }
        protected override IEnumerable<string> GetItemFilter(File item)
        {
            yield return item.Name;

        }

    }
}
