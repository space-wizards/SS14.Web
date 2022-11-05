# Space Station 14 Web Services

**These are backend services hosted by Space Wizards for all of Space Station 14 and Robust. You do not need need to host these yourself in any case (except if you feel like contributing, I guess).**

This repo contains various frontend and backend web services used by **Space Station 14**.

List of the projects in question:

* `SS14.Auth`: Auth API server used by launcher and game servers/clients.
* `SS14.ServerHub`: Public game server listing hub used by launcher.
* `SS14.Web`: (TODO) Main website for SS14 including account management and blog.

## Development

To set up a dev environment, you'll want to do the following things. Look, I know this isn't easy, but be glad I at least wrote it down for you:

* Have a local PostgreSQL database running. (and maybe a tool like pgAdmin to manage it)
* Create a database and username on that database.
* Use `dotnet ef migrations script` to get the SQL schema for the database, run it through the database and make sure you don't screw up the permissions on the created tables.
* Create an `appsettings.Secret.yml` file in both `SS14.Auth` and `SS14.Web` to hold some local preferences. Obviously substitute DB credentials or whatever:

```yaml
ConnectionStrings:
  DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14-web-test;User Id=ss14-web;Password=HelloIAmAPassword"

Mutex:
  # Change this to something local on disk.
  DbPath: 'C:\Users\Pieter-Jan Briers\Projects\ss14\web\mutex.db'
``` 

* Create the mutex DB mentioned above manually, and run `init_mutex.sql` on it. (I recommend https://sqlitebrowser.org/ for this task)
* If I didn't forget anything you should now be able to start both services and it should work:tm:.
