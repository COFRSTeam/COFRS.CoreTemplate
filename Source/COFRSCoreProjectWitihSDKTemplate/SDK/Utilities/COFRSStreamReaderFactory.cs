using System.IO;
using System.Text;

namespace $safeprojectname$.Utilities
{
    /// <summary>
    /// Stream Reader Factory
    /// </summary>
    public class COFRSStreamReaderFactory
    {
        /// <summary>
        /// Constructs a stream reader
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> that holds the incoming data.</param>
        /// <param name="encoding">The <see cref="Encoding"/> using to represent the data.</param>
        /// <returns></returns>
        public TextReader CreateReader(Stream stream, Encoding encoding)
        {
            return new StreamReader(stream, encoding);
        }
    }
}
