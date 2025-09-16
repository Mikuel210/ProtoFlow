
with open("src/venv.py", 'r', encoding = "utf-8") as file:
    application_controller_template = file.read()

statify.compile({
    "input_path": f"src/template.html",
    "output_path": f"public/index.html",
    "venv_template": application_controller_template
})