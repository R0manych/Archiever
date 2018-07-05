using ArchieverApp.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ArchieverApp.GZip
{
    public abstract class BaseGzip
    {
        #region Members

        protected const int _blockSize = 10485760; //10 MB

        protected FileStream _fsReader;

        protected FileStream _fsWriter;

        protected string _sourceFile;
        protected string _destinationFile;

        protected bool _isCancelled;
        protected bool _isSucceded;

        protected static int _threads = Environment.ProcessorCount;

        protected int _finishedThreads = 0;

        protected ManualResetEvent[] _doneEvents = new ManualResetEvent[_threads];

        protected QueueManager _queueWriter = new QueueManager();

        protected static Mutex _mutexReader = new Mutex();

        protected static Mutex _mutexWriter = new Mutex();

        protected BlockManager _blockManager = new BlockManager();

        #endregion

        #region Properties

        public bool IsSucceeded => _isSucceded;

        #endregion

        #region Constructors

        public BaseGzip(string sourceFile, string destinationFile)
        {
            _sourceFile = sourceFile;
            _destinationFile = destinationFile;
            _fsReader = new FileStream(_sourceFile, FileMode.Open);            
        }

        #endregion

        #region Methods

        public abstract void Launch();

        public int CallBackResult() => !_isCancelled && _isSucceded ? 0 : 1;

        public void Cancel()
        {
            _isCancelled = true;
        }

        protected virtual void SetDone(int i)
        {
            _doneEvents[i].Set();
            _finishedThreads++;
            if (_finishedThreads == _threads)
            {
                _fsReader.Close();
                _fsWriter.Close();
            }
        }

        #endregion
    }
}
