using ArchieverApp.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;

namespace ArchieverApp.GZip
{
    public class Decompressor : BaseGzip
    {
        #region Members

        int _counter = 0;

        #endregion

        #region Constructors

        public Decompressor(string input, string output) : base(input, output)
        {
            _fsWriter = new FileStream(_destinationFile, FileMode.Append);
        }

        #endregion

        #region Methods

        protected override void Read()
        {
            try
            {
                while (!_isCancelled)
                {
                    if (_fsReader.Position < _fsReader.Length)
                    {
                        byte[] lengthBuffer = new byte[8];
                        _fsReader.Read(lengthBuffer, 0, lengthBuffer.Length);
                        var blockLength = BitConverter.ToInt32(lengthBuffer, 4);
                        byte[] compressedData = new byte[blockLength];
                        lengthBuffer.CopyTo(compressedData, 0);

                        _fsReader.Read(compressedData, 8, blockLength - 8);
                        var dataSize = BitConverter.ToInt32(compressedData, blockLength - 4);
                        byte[] lastBuffer = new byte[dataSize];

                        var block = new ByteBlock(_counter, lastBuffer, compressedData);
                        _counter++;
                        _queueReader.Enqueue(block);
                    }
                    else
                    {
                        _queueReader.Stop();
                        _fsReader.Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowException(new Exception("Error in reading thread", ex));
            }
        }

        protected override void Compress(object i)
        {
            try
            {
                while (!_isCancelled)
                {
                    var block = _queueReader.Dequeue();

                    if (block == null)
                    {
                        SetDone((int)i);
                        return;
                    }

                    using (var ms = new MemoryStream(block.CompressedBuffer))
                    {
                        using (var gzs = new GZipStream(ms, CompressionMode.Decompress))
                        {
                            gzs.Read(block.Buffer, 0, block.Buffer.Length);
                            var decompressedData = block.Buffer;
                            block = new ByteBlock(block.ID, decompressedData);
                            _queueWriter.Enqueue(block);                            
                        }                        
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowException(new Exception($"Error in decompressing thread number {i}. \n Error description: { ex.Message}", ex));
                return;
            }
        }

        protected override void Write()
        {
            try
            {
                while (!_isCancelled)
                {
                    var block = _queueWriter.Dequeue();
                    if (block == null)
                    {
                        _fsWriter.Close();
                        return;
                    }

                    _fsWriter.Write(block.Buffer, 0, block.Buffer.Length);
                }
            }
            catch (Exception ex)
            {
                ThrowException(new Exception("Error in writing thread", ex));
                return;
            }
        }

        #endregion
    }
}
