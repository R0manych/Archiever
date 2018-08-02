using ArchieverApp.Models;
using System;
using System.IO;
using System.IO.Compression;
using System.Threading;

namespace ArchieverApp.GZip
{
    public class Compressor : BaseGzip
    {   
        #region Constructors

        public Compressor(string sourceFile, string destinationFile) : base(sourceFile, destinationFile)
        {
            _fsWriter = new FileStream(_destinationFile + ".gz", FileMode.Append);
        }

        #endregion

        #region Methods

        public override void Launch()
        {
            for (var i = 0; i < _threads; i++)
            {
                _doneEvents[i] = new ManualResetEvent(false);
                ThreadPool.QueueUserWorkItem(Compress, i);
            }

            WaitHandle.WaitAll(_doneEvents);

            _isSucceded = !_isCancelled;            
        }

        public byte[] Read()
        {
            try
            {
                int bytesRead;
                byte[] lastBuffer;

                if (_fsReader.Position < _fsReader.Length && !_isCancelled)
                {
                    if (_fsReader.Length - _fsReader.Position <= _blockSize)
                    {
                        bytesRead = (int)(_fsReader.Length - _fsReader.Position);
                    }
                    else
                    {
                        bytesRead = _blockSize;
                    }

                    lastBuffer = new byte[bytesRead];
                    _fsReader.Read(lastBuffer, 0, bytesRead);

                    return lastBuffer;
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

        private void Compress(object i)
        {
            try
            {
                while (true && !_isCancelled)
                {
                    _mutexReader.WaitOne();
                    var buffer = Read();
                    _mutexReader.ReleaseMutex();

                    if (buffer == null)
                    {
                        SetDone((int)i);
                        return;
                    }
                    var block = _blockManager.CreateByteBlock(buffer);

                    using (var ms = new MemoryStream())
                    {
                        using (var czs = new GZipStream(ms, CompressionMode.Compress))
                        {
                            czs.Write(block.Buffer, 0, block.Buffer.Length);
                        }

                        byte[] compressedData = ms.ToArray();

                        var outdata = new ByteBlock(block.ID, compressedData);
                        _queueWriter.Enqueue(outdata);
                    }
                    _mutexWriter.WaitOne();
                    Write();
                    _mutexWriter.ReleaseMutex();
                }                 
                
            }
            catch (Exception ex)
            {
                ThrowException(new Exception($"Error in compressing thread number {i}. \n Error description: { ex.Message}", ex));
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

                BitConverter.GetBytes(block.Buffer.Length).CopyTo(block.Buffer, 4);
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
