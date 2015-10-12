# neovim.cs
<<<<<<< HEAD
A C# client for talking to neovim.
=======
A C# client for talking to [Neovim.](https://github.com/neovim/neovim)
This also includes a WPF terminal (wip)

To use the client simply create a new NeovimClient instance with the path to the neovim executable as argument.
This creates a new neovim process and takes over it's Input and Output.
You can then use all function bindings from the NeovimClient class instance to communicate with neovim.
The Notifications neovim sends are currently in [MessagePackObject](https://github.com/msgpack/msgpack-cli/wiki/Messagepackobject) format from Messagepack-CLI.
>>>>>>> nv_wip
