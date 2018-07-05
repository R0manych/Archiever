using ArchieverApp.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchieverApp.Utils
{
    public class BlockManager
    {
        private int _blockId;

        private object _locker = new object();

        public ByteBlock CreateByteBlock(byte[] buffer)
        {
            lock (_locker)
            {
                var block = new ByteBlock(_blockId, buffer);
                _blockId++;
                return block;
            }
        }
    }
}
