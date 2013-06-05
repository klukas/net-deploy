Deploy websites by pushing to git
=================================

How it works
------------
Runs as an IIS website that can update other websites (including itself!) on the same box. It can:

1. Pull changes from a git repo
2. Install any nuget packages required in packages.config files
3. Build from source (.NET 4)
4. Deploy the built app

Prerequisites
-------------

1. Install git, and make sure the git bin directory is on the system path (not just your user path, and not the git cmd directory).
   e.g. `setx /m path "%path%;c:\program files (x86)\git\bin"`
2. Install the [NuGet.exe command line bootstrapper](http://nuget.codeplex.com/releases/view/58939), put it in the system path also.
3. Copy `C:\Program Files (x86)\MSBuild` to 

Setting up the website
----------------------

1. Build net-deploy from source. Copy the files to your server, e.g. `c:\inetpub\deploy`.
2. In the IIS management console...
	1. create a new website (or an application under an existing website) that points to the net-deploy files.
	2. If you created an application instead of a website, create a new application pool for the application.
	3. In the application pool advanced settings, set it to run as "LOCAL SYSTEM" so that it has permissions to update the file system.
3. In the appsettings.config, you'll want to change the password (default is 'password') and probably require HTTPS if you're
running in production.

Configuring apps
----------------

Now you need to configure the apps that net-deploy can deploy. A good example is setting up net-deploy to deploy itself

1. Create a folder in App_Data in the net-deploy web folder called 'net-deploy'
2. Inside this folder, create a file called config.txt
3. Add this configuration to config.txt

    ```
    git = git://github.com/lukesampson/net-deploy.git
    deploy_ignore = *.cs *.sln *.csproj *.log *.ps1 .git .gitignore obj thumbs.db App_Data appsettings.config
    deploy_to = C:\inetpub\sites\deploy
    ```

Trying it out
-------------

Open the website in your web browser and log in. If you configured net-deploy as above you should see one app, 'net-deploy'. Click on it, and then click 'build' to test that it's working.

If something goes wrong, you can click on 'see build log' to see the output from the build.


Troubleshooting
---------------

**Build hangs when pulling or cloning from git**

First, open task manager and kill the ssh.exe process: the build will fail and you can get on with fixing it.

What's probably happening is, git is spawning SSH, which is doing some interactive stuff--asking you to confirm the key for the remote host or looking for your private key in %userprofile%.

You don't see any useful output messages because git spawns ssh.exe as a separate process, so the SSH output isn't captured.

There are 3 ways to get it working.

**If the repo is public:**

1. Change your config.txt to use a public read-access URL that doesn't require authentication for the `git` setting. GitHub provides these for public repos.

**If the repo is private:**

2. **Easier, but stores passwords in plain text**: use a non-SSH git URL containing a username and password for the `git` setting in config.txt, e.g. `https://username:password@github.com/username/yourrepo.git`.
3. **More setup, slightly more secure**: since net-deploy is running git under the local system account, it's going to have trouble finding your SSH keys. You can put them in the git installation directory as a fallback.
    1. Create an SSH key without a password, if you don't already have one.
	2. Put your password-less SSH key in `%programfiles(x86)%\Git\.ssh\id_rsa`
	3. Copy your `%userprofile%\.ssh\known_hosts` file (could be from another computer) to `%programfiles(x86)%\Git\.ssh\known_hosts`

If it's still not working, you might need to do some debugging by running as the local system account. [This blog post](http://blogs.msdn.com/b/adioltean/archive/2004/11/27/271063.aspx) lists some hacks to get it done. See also [this question on stackoverflow](http://stackoverflow.com/questions/77528/how-do-you-run-cmd-exe-under-the-local-system-account).
