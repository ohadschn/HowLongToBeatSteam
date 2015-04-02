using System;
using Microsoft.Practices.EnterpriseLibrary.TransientFaultHandling;

namespace Common.Util
{
    public class GenericTransientErrorDetectionStrategy : ITransientErrorDetectionStrategy
    {
        private readonly Predicate<Exception> m_transientErrorDetector;

        public GenericTransientErrorDetectionStrategy(Predicate<Exception> transientErrorDetector)
        {
            m_transientErrorDetector = transientErrorDetector ?? (ex => true);
        }
               
        public bool IsTransient(Exception ex)
        {
            return m_transientErrorDetector(ex);
        }
    }
}
