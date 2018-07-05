using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArchieverApp.Models
{
    public class ByteBlock
    {
        #region Members

        private int _id;
        private byte[] _buffer;
        private byte[] _compressedBuffer;

        #endregion

        #region Properties

        public int ID => _id;
        public byte[] Buffer => _buffer;
        public byte[] CompressedBuffer => _compressedBuffer;

        #endregion

        #region Constructors

        public ByteBlock(int id, byte[] buffer) : this(id, buffer, new byte[0])
        {

        }

        public ByteBlock(int id, byte[] buffer, byte[] compressedBuffer)
        {
            _id = id;
            _buffer = buffer;
            _compressedBuffer = compressedBuffer;
        }

        #endregion
    }
}
