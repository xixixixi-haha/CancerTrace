using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CancerTrace.Data.Validation
{
    public sealed class DataValidationResult
    {
        private readonly List<string> errors = new List<string>();
        private ReadOnlyCollection<string> readOnlyErrors;

        public bool IsValid
        {
            get { return errors.Count == 0; }
        }

        public IReadOnlyList<string> Errors
        {
            get
            {
                if (readOnlyErrors == null)
                {
                    readOnlyErrors = errors.AsReadOnly();
                }

                return readOnlyErrors;
            }
        }

        internal void Add(string fileName, string fieldPath, object expected, object actual)
        {
            errors.Add(
                fileName + " | " + fieldPath + " | expected: " +
                (expected == null ? "null" : expected.ToString()) +
                " | actual: " + (actual == null ? "null" : actual.ToString()));
        }
    }
}
