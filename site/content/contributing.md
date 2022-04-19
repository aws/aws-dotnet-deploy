## Build and Test Documentation

### Install Material for MkDocs
Material for MkDocs is a theme for MkDocs, a static site generator geared towards (technical) project documentation. If you're familiar with Python, you can install Material for MkDocs with pip, the Python package manager.

```
pip install mkdocs-material
```
For, other installation options [see here](https://squidfunk.github.io/mkdocs-material/getting-started/)

### Deploying to a Local Server
MkDocs comes with a built-in dev-server that lets you preview your documentation as you work on it. 

From the root of the project repository, run the following command:
```
mkdocs serve
```

Paste the link to the local server on a web browser to look at the documentation.

The dev-server also supports auto-reloading, and will rebuild your documentation whenever anything in the configuration file, documentation directory, or theme directory changes.