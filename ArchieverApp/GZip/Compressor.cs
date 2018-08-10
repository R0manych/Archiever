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

        protected override void Read()
        {
            try
            {
                while (!_isCancelled)
                {
                    int bytesRead;
                    byte[] lastBuffer;

                    if (_fsReader.Position < _fsReader.Length)
                    {
                        if (_queueReader.Count < _threads)
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

                            _queueReader.Enqueue(_blockManager.CreateByteBlock(lastBuffer));
                        }
                        else
                        {
                            Thread.Sleep(5);
                        }
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
                return;
            }
        }

        protected override void Compress(object i)
        {
            try
            {
                while (!_isCancelled)
                {
                    var block = _queueReader.Dequeue();
                    if (block != null)
                    {
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
                    }
                    else
                    {
                        SetDone((int)i);
                        return;
                    }
                }             
            }
            catch (Exception ex)
            {
                ThrowException(new Exception($"Error in compressing thread number {i}. \n Error description: { ex.Message}", ex));
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

                    BitConverter.GetBytes(block.Buffer.Length).CopyTo(block.Buffer, 4);
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
