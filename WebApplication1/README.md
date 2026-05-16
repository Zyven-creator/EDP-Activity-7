# Student Information System

This project has two parts:

- `WebApplication1` - the browser frontend at `http://localhost:5161`
- `StudentInfoSystemAPI` - the backend API at `http://localhost:5169`

## Requirements

- .NET 10 SDK
- MySQL running locally
- Database named `act_edp`

## Default Login

- Username: `admin`
- Password: `admin123`

## Database Connection

The API uses this connection string in [StudentInfoSystemAPI/appsettings.json](C:\Users\francIs josh rosales\Act4EDP\WebApplication1\StudentInfoSystemAPI\appsettings.json:1):

```json
"ConnectionStrings": {
  "DefaultConnection": "server=localhost;database=act_edp;uid=root;pwd=;"
}
```

Update it if your MySQL username or password is different.

## Run The API

Open a terminal in:

```powershell
cd "C:\Users\francIs josh rosales\Act4EDP\WebApplication1"
```

Then run:

```powershell
$env:APPDATA="$PWD\.appdata"
dotnet run --project .\StudentInfoSystemAPI\StudentInfoSystemAPI.csproj --launch-profile http
```

The API should start at:

- `http://localhost:5169`

You can test it here:

- [http://localhost:5169/openapi/v1.json](http://localhost:5169/openapi/v1.json)

## Run The Website

Open a second terminal in:

```powershell
cd "C:\Users\francIs josh rosales\Act4EDP\WebApplication1"
```

Then run:

```powershell
$env:APPDATA="$PWD\.appdata"
dotnet run --project .\WebApplication1.csproj --launch-profile http
```

Open the site here:

- [http://localhost:5161](http://localhost:5161)

## Normal Startup Order

1. Start MySQL
2. Start `StudentInfoSystemAPI`
3. Start `WebApplication1`
4. Open `http://localhost:5161`
5. Log in with `admin` / `admin123`

## Common Problems

### `Unable to reach the API. Failed to fetch`

Usually means the API is not running on `http://localhost:5169`.

Check the API first:

- confirm the API terminal says it is listening on `http://localhost:5169`
- open [http://localhost:5169/openapi/v1.json](http://localhost:5169/openapi/v1.json)

### Website build errors about `System.Windows.Forms`

Those come from the old desktop-form files. The web project is already configured to exclude them. If you still see stale build issues, clean and rerun:

```powershell
Remove-Item .\bin, .\obj -Recurse -Force
dotnet run --project .\WebApplication1.csproj --launch-profile http
```

### Database connection errors

Check:

- MySQL is running
- database `act_edp` exists
- your tables exist: `students`, `courses`, `enrollments`
- the username/password in `StudentInfoSystemAPI/appsettings.json` are correct

## Project Notes

- The frontend lives in [wwwroot](C:\Users\francIs josh rosales\Act4EDP\WebApplication1\wwwroot:1)
- The API lives in [StudentInfoSystemAPI](C:\Users\francIs josh rosales\Act4EDP\WebApplication1\StudentInfoSystemAPI:1)
- The old WinForms files are still in the repo for reference, but the browser app is the one that should be run
