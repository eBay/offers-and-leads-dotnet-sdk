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

namespace EBay.OffersAndLeads.Configuration;

internal sealed class AsyncLocalTokenContext : ITokenContext
{
    private readonly AsyncLocal<string?> _current = new();

    public string? Current => _current.Value;

    public async Task<T> RunAsync<T>(string? token, Func<Task<T>> action)
    {
        var previous = _current.Value;
        _current.Value = token;
        try { return await action(); }
        finally { _current.Value = previous; }
    }

    public async Task RunAsync(string? token, Func<Task> action)
    {
        var previous = _current.Value;
        _current.Value = token;
        try { await action(); }
        finally { _current.Value = previous; }
    }
}
