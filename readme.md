Deploy websites by pushing to git
=================================

How it works
------------
Runs as an IIS website that can update other websites (including itself!) on the same box. It can:

1. Pull changes from a git repo
2. Install any nuget packages required in packages.config files
3. Build from source (.NET 4)
4. Deploy the built app

1. Prerequisites
----------------

1. Install git, and make sure the git bin directory is on the system path (not just your user path, and not the git cmd directory).
   e.g. `setx /m path "%path%;c:\program files (x86)\git\bin"
2. Install targeting packs for MS Build (see [here](http://stackoverflow.com/a/3315614/87453)).

2. Setting up the website
-------------------------

1. Build net-deploy from source. Copy the files to your server, e.g. `c:\inetpub\wwwroot\deploy`.
2. In the IIS management console, create a new website (or an application under an existing website) that points to the net-deploy files.
3. If you created an application instead of a website, create a new application pool for the application.
4. In the application pool advanced settings, set it to run as "LOCAL SYSTEM" so that it has permissions to update the file system.

3. Configuration
----------------

Now you need to configure the apps that net-deploy can deploy. A good example is setting up net-deploy to deploy itself

1. Create a folder in App_Data in the net-deploy web folder called 'net-deploy'
2. Inside this folder, create a file called config.txt
3. Add this configuration to config.txt
    
	git = git://github.com/lukesampson/net-deploy.git
    deploy_ignore = *.cs .gitignore *.sln *.csproj *.log .git obj thumbs.db App_Data
    deploy_to = C:\inetpub\wwwroot\deploy


4. Try it out
-------------

Open the website in your web browser and log in. If you configured net-deploy as above you should see one app, 'net-deploy'. Click on it, and then click 'build' to test that it's working.

If something goes wrong, you can click on 'see build log' to see the output from the build.


5. Troubleshooting
------------------

**Build hangs when pulling or cloning from git**
Open task manager and kill the ssh.exe process: the build will then fail. What's probably happening is, SSH is looking for your private key in %userprofile%

