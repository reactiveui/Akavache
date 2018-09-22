using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace System.IO
{
    public static class MicrosoftStreamExtensions
    {
        /// <summary>
        /// Ases the random access stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        public static IRandomAccessStream AsRandomAccessStream(this Stream stream)
        {
            return new RandomStream(stream);
        }

        /// <summary>
        /// Ases the random access stream.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        /// <returns></returns>
        public static IRandomAccessStream AsRandomAccessStream(this byte[] bytes)
        {
            return new RandomStream(bytes);
        }
    }

    public class RandomStream : IRandomAccessStream
    {
        private Stream streamValue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStream"/> class.
        /// </summary>
        /// <param name="stream">The stream.</param>
        public RandomStream(Stream stream)
        {
            streamValue = stream;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStream"/> class.
        /// </summary>
        /// <param name="bytes">The bytes.</param>
        public RandomStream(byte[] bytes)
        {
            streamValue = new MemoryStream(bytes);
        }

        /// <summary>
        /// Gets the input stream at.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public IInputStream GetInputStreamAt(ulong position)
        {
            if ((long)position > streamValue.Length) {
                throw new IndexOutOfRangeException();
            }

            streamValue.Position = (long)position;

            return streamValue.AsInputStream();
        }

        /// <summary>
        /// Gets the output stream at.
        /// </summary>
        /// <param name="position">The position.</param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
        public IOutputStream GetOutputStreamAt(ulong position)
        {
            if ((long)position > streamValue.Length) {
                throw new IndexOutOfRangeException();
            }

            streamValue.Position = (long)position;

            return streamValue.AsOutputStream();
        }

        /// <summary>
        /// Gets or sets the size.
        /// </summary>
        /// <value>The size.</value>
        public ulong Size
        {
            get
            {
                return (ulong)streamValue.Length;
            }
            set
            {
                streamValue.SetLength((long)value);
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can read.
        /// </summary>
        /// <value><c>true</c> if this instance can read; otherwise, <c>false</c>.</value>
        public bool CanRead
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance can write.
        /// </summary>
        /// <value><c>true</c> if this instance can write; otherwise, <c>false</c>.</value>
        public bool CanWrite
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Clones the stream.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Gets the position.
        /// </summary>
        /// <value>The position.</value>
        public ulong Position
        {
            get
            {
                return (ulong)streamValue.Position;
            }
        }

        /// <summary>
        /// Seeks the specified position.
        /// </summary>
        /// <param name="position">The position.</param>
        public void Seek(ulong position)
        {
            streamValue.Seek((long)position, 0);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        public void Dispose()
        {
            streamValue.Dispose();
        }

        /// <summary>
        /// Reads the asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="count">The count.</param>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// Flushes the asynchronous.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IAsyncOperation<bool> FlushAsync()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Writes the asynchronous.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            throw new NotImplementedException();
        }
    }
}

namespace System.IO.IsolatedStorage
{
    internal class IsolatedStorageException : Exception { }
}
