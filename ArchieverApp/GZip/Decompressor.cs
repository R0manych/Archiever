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

        public override void Launch()
        {
            for (int i = 0; i < _threads; i++)
            {
                _doneEvents[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(Decompress, i);
            }

            WaitHandle.WaitAll(_doneEvents);

            _isSucceded  = !_isCancelled;
        }

        private ByteBlock Read()
        {
            try
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
                    return block;
                }
                else
                    return null;
            }
            catch (Exception ex)
            {
                ThrowException(new Exception("Error in reading thread", ex));
                return null;
            }
        }

        private void Decompress(object i)
        {
            try
            {
                while (true && !_isCancelled)
                {
                    _mutexReader.WaitOne();
                    var block = Read();
                    _mutexReader.ReleaseMutex();

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
                        _mutexWriter.WaitOne();
                        Write();
                        _mutexWriter.ReleaseMutex();
                    }
                }
            }
            catch (Exception ex)
            {
                ThrowException(new Exception($"Error in decompressing thread number {i}. \n Error description: { ex.Message}", ex));
                return;
            }
        }

        private void Write()
        {
            try
            {
                var block = _queueWriter.Dequeue();
                if (block == null)
                    return;

                _fsWriter.Write(block.Buffer, 0, block.Buffer.Length);
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
