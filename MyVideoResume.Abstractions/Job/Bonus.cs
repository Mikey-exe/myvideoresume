﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyVideoResume.Abstractions.Job;

public enum BonusType
{
    Dollar,
    Percentage
}
public enum BonusFrequency
{
    Annual,
    Quarterly,
    Monthly
}

public class Bonus
{
    public string UserId { get; set; }
    public string Description { get; set; }
    public BonusType BonusType { get; set; }
    public BonusFrequency BonusFrequency { get; set; }
}
