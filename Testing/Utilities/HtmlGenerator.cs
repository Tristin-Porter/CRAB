namespace CRAB.WebAssembly;

/// <summary>
/// Generates HTML files for running WebAssembly modules in the browser.
/// </summary>
public static class HtmlGenerator
{
    /// <summary>
    /// Generate an HTML file that loads and runs a WebAssembly module.
    /// </summary>
    /// <param name="title">Page title</param>
    /// <param name="wasmFileName">Name of the .wasm file</param>
    /// <param name="jsFileName">Name of the .js wrapper file</param>
    /// <returns>HTML content</returns>
    public static string GenerateHtml(string title, string wasmFileName, string jsFileName)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{title}</title>
    <style>
        body {{
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            max-width: 900px;
            margin: 40px auto;
            padding: 20px;
            background: #f5f5f5;
        }}
        .container {{
            background: white;
            border-radius: 8px;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }}
        h1 {{
            color: #333;
            border-bottom: 3px solid #4CAF50;
            padding-bottom: 10px;
        }}
        #output {{
            background: #1e1e1e;
            color: #d4d4d4;
            padding: 20px;
            border-radius: 4px;
            font-family: 'Consolas', 'Monaco', monospace;
            min-height: 200px;
            margin-top: 20px;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}
        .status {{
            padding: 10px;
            margin: 15px 0;
            border-radius: 4px;
        }}
        .success {{
            background: #d4edda;
            color: #155724;
            border: 1px solid #c3e6cb;
        }}
        .error {{
            background: #f8d7da;
            color: #721c24;
            border: 1px solid #f5c6cb;
        }}
        .info {{
            background: #d1ecf1;
            color: #0c5460;
            border: 1px solid #bee5eb;
        }}
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🦀 CRAB - {title}</h1>
        <div id=""status"" class=""status info"">Loading WebAssembly module...</div>
        <h2>Console Output</h2>
        <div id=""output""></div>
    </div>

    <script src=""{jsFileName}""></script>
    <script>
        const outputDiv = document.getElementById('output');
        const statusDiv = document.getElementById('status');

        // Override console.log to display in the page
        const originalLog = console.log;
        console.log = function(...args) {{
            originalLog.apply(console, args);
            const message = args.map(arg => 
                typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
            ).join(' ');
            outputDiv.textContent += message + '\\n';
        }};

        // Override console.error to display in the page
        const originalError = console.error;
        console.error = function(...args) {{
            originalError.apply(console, args);
            const message = args.map(arg => 
                typeof arg === 'object' ? JSON.stringify(arg, null, 2) : String(arg)
            ).join(' ');
            outputDiv.textContent += 'ERROR: ' + message + '\\n';
            statusDiv.className = 'status error';
            statusDiv.textContent = '❌ Error: ' + message;
        }};

        // Wait for the WASM module to load (the JS wrapper handles this automatically)
        setTimeout(() => {{
            if (statusDiv.className === 'status info') {{
                statusDiv.className = 'status success';
                statusDiv.textContent = '✅ WebAssembly module loaded and executed successfully!';
            }}
        }}, 100);
    </script>
</body>
</html>";
    }
}
