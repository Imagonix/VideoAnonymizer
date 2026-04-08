using System;
using System.Collections.Generic;
using System.Text;

namespace VideoAnonymizer.Database
{
    public class SystemSetting : EntityBase
    {
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public DateTime UpdatedAtUtc { get; set; }
    }
}
