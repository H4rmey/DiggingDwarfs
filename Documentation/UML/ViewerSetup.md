# UML Documentation Viewer Setup

This guide provides multiple options for viewing the UML diagrams in a browser.

## Option 1: Use GitHub Pages (Recommended)

If your project is on GitHub, you can use GitHub Pages to automatically render the Mermaid diagrams:

1. Push your repository to GitHub
2. Go to Settings > Pages
3. Select the main branch and /docs folder (or wherever your Documentation folder is)
4. Enable GitHub Pages

GitHub will automatically render the Mermaid diagrams in the markdown files.

## Option 2: Local HTML Viewer

You can create a simple HTML viewer locally by following these steps:

1. Create a file named `index.html` in the Documentation/UML directory with the following content:

```html
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>DiggingDwarfs UML Documentation</title>
    <script src="https://cdn.jsdelivr.net/npm/mermaid@10.6.1/dist/mermaid.min.js"></script>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 0;
            display: flex;
            height: 100vh;
        }
        .sidebar {
            width: 250px;
            background-color: #f5f5f5;
            padding: 20px;
            overflow-y: auto;
            box-shadow: 2px 0 5px rgba(0,0,0,0.1);
        }
        .sidebar h1 {
            font-size: 1.5em;
            margin-top: 0;
            margin-bottom: 20px;
        }
        .sidebar ul {
            padding-left: 20px;
        }
        .sidebar li {
            margin-bottom: 10px;
        }
        .sidebar a {
            text-decoration: none;
            color: #333;
        }
        .sidebar a:hover {
            color: #0066cc;
        }
        .content {
            flex-grow: 1;
            padding: 20px;
            overflow-y: auto;
        }
        .hidden {
            display: none;
        }
    </style>
</head>
<body>
    <div class="sidebar">
        <h1>UML Documentation</h1>
        <ul>
            <li><a href="#" onclick="loadContent('overview')">Architecture Overview</a></li>
            <li><a href="#" onclick="loadContent('physics')">Physics System</a></li>
            <li><a href="#" onclick="loadContent('rendering')">Rendering & Input</a></li>
            <li><a href="#" onclick="loadContent('pixel-update')">Pixel Update Flow</a></li>
            <li><a href="#" onclick="loadContent('user-interaction')">User Interaction Flow</a></li>
        </ul>
    </div>
    <div class="content" id="content">
        <div id="overview">
            <h1>DiggingDwarfs Architecture Overview</h1>
            <p>Please select a diagram from the sidebar to view detailed UML diagrams.</p>
            <p>This viewer allows you to browse the project architecture documentation
               with proper rendering of Mermaid diagrams.</p>
        </div>
        <div id="physics" class="hidden"></div>
        <div id="rendering" class="hidden"></div>
        <div id="pixel-update" class="hidden"></div>
        <div id="user-interaction" class="hidden"></div>
    </div>

    <script>
        // Initialize Mermaid
        mermaid.initialize({ startOnLoad: true });
        
        // Function to load content into the viewer
        async function loadContent(section) {
            // Hide all sections
            document.querySelectorAll('.content > div').forEach(div => {
                div.classList.add('hidden');
            });
            
            // Show the selected section
            const sectionElement = document.getElementById(section);
            sectionElement.classList.remove('hidden');
            
            // If the section is empty, load the content
            if (sectionElement.childElementCount <= 1) {
                let filePath;
                switch(section) {
                    case 'overview':
                        filePath = 'ArchitectureOverview.md';
                        break;
                    case 'physics':
                        filePath = 'ClassDiagram-Physics.md';
                        break;
                    case 'rendering':
                        filePath = 'ClassDiagram-RenderingInput.md';
                        break;
                    case 'pixel-update':
                        filePath = 'SequenceDiagram-PixelUpdate.md';
                        break;
                    case 'user-interaction':
                        filePath = 'SequenceDiagram-UserInteraction.md';
                        break;
                    default:
                        return;
                }
                
                try {
                    const response = await fetch(filePath);
                    const markdown = await response.text();
                    
                    // Convert markdown to HTML (simple version)
                    let html = markdown
                        .replace(/^# (.*$)/gm, '<h1>$1</h1>')
                        .replace(/^## (.*$)/gm, '<h2>$1</h2>')
                        .replace(/^### (.*$)/gm, '<h3>$1</h3>')
                        .replace(/\*\*(.*?)\*\*/g, '<strong>$1</strong>')
                        .replace(/\*(.*?)\*/g, '<em>$1</em>')
                        .replace(/\n/g, '<br>');
                    
                    // Handle Mermaid blocks
                    html = html.replace(/```mermaid\s*([\s\S]*?)```/g, 
                        '<pre class="mermaid">$1</pre>');
                    
                    sectionElement.innerHTML = html;
                    
                    // Re-render Mermaid diagrams
                    mermaid.init(undefined, document.querySelectorAll('.mermaid'));
                    
                } catch (error) {
                    sectionElement.innerHTML = `<h2>Error loading content</h2><p>${error.message}</p>`;
                }
            }
        }
        
        // Load overview by default
        loadContent('overview');
    </script>
</body>
</html>
```

2. Create a simple script to serve the files locally. For example, using Python:

Create a file named `serve.py` with:

```python
# Simple HTTP server for viewing UML documentation
import http.server
import socketserver

PORT = 8000
Handler = http.server.SimpleHTTPRequestHandler

with socketserver.TCPServer(("", PORT), Handler) as httpd:
    print(f"Serving at http://localhost:{PORT}")
    print(f"Open your browser to http://localhost:{PORT}/index.html")
    httpd.serve_forever()
```

3. Run the script from the Documentation/UML directory:

```
cd Documentation/UML
python serve.py
```

4. Open your browser to http://localhost:8000/index.html

## Option 3: VS Code Extensions

You can also view the diagrams directly in VS Code:

1. Install the "Markdown Preview Mermaid Support" extension
2. Open any of the .md files
3. Use the "Open Preview" feature (Ctrl+Shift+V)

## Option 4: Online Mermaid Editor

For quick viewing of individual diagrams:

1. Copy the Mermaid code from any diagram
2. Paste it into https://mermaid.live/
3. The diagram will be rendered instantly

## Option 5: Documentation Generator

For more advanced documentation needs, consider using a documentation generator like MkDocs with the Material theme:

1. Install MkDocs: `pip install mkdocs mkdocs-material`
2. Create a mkdocs.yml file in your project root:

```yaml
site_name: DiggingDwarfs Documentation
theme:
  name: material
  features:
    - navigation.tabs
    - navigation.sections
    - toc.integrate
    - content.code.copy
markdown_extensions:
  - pymdownx.superfences:
      custom_fences:
        - name: mermaid
          class: mermaid
          format: !!python/name:pymdownx.superfences.fence_code_format
nav:
  - Home: index.md
  - Architecture:
      - Overview: UML/ArchitectureOverview.md
      - Physics System: UML/ClassDiagram-Physics.md
      - Rendering & Input: UML/ClassDiagram-RenderingInput.md
      - Pixel Update Flow: UML/SequenceDiagram-PixelUpdate.md
      - User Interaction: UML/SequenceDiagram-UserInteraction.md
```

3. Create a docs folder and move your Documentation/UML files there
4. Run `mkdocs serve` to preview the documentation
5. Run `mkdocs build` to generate the static site

This will create a professional documentation site with proper navigation and rendering of all Mermaid diagrams.