.PHONY: debug build run

# Hot-reload dev loop: edits to .razor / .cs are applied to the running app.
# Rude edits (ones hot reload can't patch) restart the app instead of prompting.
debug: export DOTNET_WATCH_RESTART_ON_RUDE_EDIT = 1
debug:
	dotnet watch run

build:
	dotnet build

run:
	dotnet run
