﻿using MyVideoResume.Abstractions.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyVideoResume.Abstractions.Resume.Formats.MyVideoResumeFormat;

public enum MetaType
{
    Hyperlink=0,
    Video=1,
    Image=2,
    Content=3
}

public class MetaData : CommonBase
{
    public string UserId { get; set; }

    public string ReferenceId { get; set; }
    public string Name { get; set; }
    public MetaType MetaType { get; set; } = MetaType.Content;
    public string Url { get; set; }
    public string Description { get; set; }
}
