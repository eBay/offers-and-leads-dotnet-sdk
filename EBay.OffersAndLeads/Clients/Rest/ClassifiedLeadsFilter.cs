// Copyright 2026 eBay Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.


namespace EBay.OffersAndLeads.Clients.Rest;

public sealed class ClassifiedLeadsFilter
{
    /// <summary>ISO 8601 lower bound on lead creation date.</summary>
    public string? StartTime { get; set; }

    /// <summary>ISO 8601 upper bound on lead creation date.</summary>
    public string? EndTime { get; set; }

    /// <summary>"Answered" or "Unanswered". Omit to return both.</summary>
    public string? Status { get; set; }

    /// <summary>"true" to include member messages in the response.</summary>
    public string? IncludeMessages { get; set; }
}
