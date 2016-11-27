using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XMovie.Models
{
    public class MovieImporterException : Exception
    {
        public enum Error
        {
            CannotCreateThumbnailDirectory,
            CannotReadFile,

            FFProbeNotFound,
            FFProbeError,
            FFMpegNotFound,

            Unknown
        }

        public Error Reason { get; private set; } 

        public MovieImporterException()
        {
            Reason = Error.Unknown;
        }

        public MovieImporterException(string message, Exception innerException)
            : base(message, innerException)
        {
            Reason = Error.Unknown;
        }

        public MovieImporterException(string message, Exception innerException, Error reason)
            : base(message, innerException)
        {
            Reason = Error.Unknown;
        }

        public MovieImporterException(string message, Error reason) : base(message)
        {
            Reason = reason;
        }
    }
}
