# Covid Alerter Backend API

## Description
This repo is part of the Covid Alerter project. It serves as the backend API and database manager. Clients can sumbit their reports, get the results, authenticate, and modify user setttings through this API

## Usage
To start the app, run `dotnet run` like you would with any other dotnet-based project.

## Technology
This project uses Asp.Net Core and Entitiy Framework Core. The app is intended to be used for MySQL/MariaDB as the user info and reports database, and a PostgreSQL database, with PostGIS support, filled with [Open Street Map](https://www.openstreetmap.org/) map data.

## File hierarchy
The main API backends are located in the `Controllers/` folder.
* Anything related to authentication and google sign-in is done through `Controllers/AuthenticationController.cs`
* The report saving mechanism is done in `Controllers/ReportsController.cs`
* Getting and modifying settings can be done with endpoints located in `Controllers/SettingsController.cs`
* Fetching the results and ranks of the neighbourhoods can be done with endpoints in the `Controllers/GetController.cs` file.

The model for the ORM database system can be found in `Models/`

## API Endpoints

### Authentication

#### `GET /auth/login?Token=<google auth token>`
Use this end points to login the user
Returns a session id you can use for the rest of the functions of the API

#### `GET /auth/logout?SessionId=<session id>`
Logout the user. The session Id will be invalidated serverside

### Reports
#### `POST /reports/sumbit?SessionId=<session id>`
Use this endpoint to submit a new report. You also need to pass a `Location` object in the body

### Settings
#### `GET /opt/get?SessionId=<session id>`
Get the stored preferences of the user
Returns a json `Settings` object.

#### `POST /opt/change?SessionId=<session id>`
Change the user preferences
Pass the full user `Settings` object as the boyd data

### Getting results

#### `GET /get/live?[id | name | lat, lon]` 
To get the Live count of a neighbourhood, pass either its OSM Id, Name, or a pair of lat long as a point inside the desired neighbourhood (you can use this to get the live count directly based on GPS coordinates)

#### `GET /get/score/single?[id | name | lat, lon]&date=<date of the score>`
Get the score for a single neighbourhood on a desired data. Neighbourhood can be identified by either its OSM Id, name, or a pair of lat lon showing a point inside the neighbourhood

#### `GET /get/score/all?[id | name | lat, lon]&date=<date of the score`
Get the average score of the neighbourhood and its child neighbourhoods

#### `GET /get/score/polygon?[id | name]&date=<date of the score>`
Get the average score of all neighbourhoods inside a polygon.
The polygon can either be a pre-defined polygon on the map, specified using its OSM Id or name
Or a pair of lat, lon as a json list inside the body content of the requst

#### `GET /get/score/batch?date=<date of the score>`
Get a list of the score for each neighbourhood. To specify the neighbourhoods, either pass a list of OSM Ids or OSM Names as a body parameter.
An optional boolean query parameter `IsName` can be provided to force the detection system to treat a list as a list of names or Ids.
Note that because of how Asp.Net Core works, boolean values can only be passed as `true` or `false`, and`0` or `1` **won't work**

An example of the raw body data for a list of OSM Ids:
```json
["546541582", "999293188"]
```

## Database format

The project consists of 2 databases:
* MySQL/MariaDB: for storing all data
* PostgreSQL + PostGis: Read-only database filled with OSM Map data for geographical processing

### MySQL database format
The MySQL database consists of 5 tables:
* `Users`: Users data
* `Reports`: Submitted reports
* `Neighbourhoods`: Indexed Neighbourhoods information
* `ScoreLogs`:  Scores for neighbourhoods on certain dates
* `ChildParents`: Many-to-many relationship table to store info about neighbourhoods and their geographical childs

#### Users table
The `Users` table consists of 6 columns:
* `Id`: Primary key
* `GoogleId`: User Google account Id, used for authentication
* `SessionId`: User's latest session Id (`null` when logged out)
* `LastInteraction`: User's last interaction time with the API, used to log out inactive users after a certain time
* `Settings`: Json string of user preferences (encoded and decoded through EF Core hooks)
* `LastLocationId`: User's latest neighbourhood id, used for live counting functionality

Each user makes up one row of this table. Users never get removed from the table as of right now. Duplicate users shouldn't be made and only the `SessionId` should be replaced

#### Reports table
The `Reports` tables consists of 6 columns:
* `Id`: Primary key
* `Longitude`: The report location longitude
* `Latitude`: The report latitude
* `UserId`: Foreign key pointing towards the User who submitted the report
* `NeighbourhoodId`: Foreign key pointing towards the neighbourhood in which the report was sent from
* `Timestamp`: The report date and time

Each submission of a report makes a new row in this table

#### Neighbourhoods table
The `Neighbourhoods` talbe consists of 8 columns:
* `Id`: Primary key
* `Name`: The name if the neighbourhood
* `LiveCount`: The current live cases count in the neighbourhood
* `OSMId`: The Id of this location in the OSM database
* `HasChilds`: Boolean value telling whether the neighbourhood has any child neighbourhoods or no
* `IsRelation`: Boolean value telling whether the neighbourhood is a `relation` or a `way` in the OSM database
* `IsBig`: Boolean value telling whether the neighbourhood is a big type (province, state, etc.) or a normal neighbourhood
* `Ratio`: How many people per square meter are required to add 1 whole point to the final severity score (this field is also referred to as `PAR` (person are ratio) in the comments of the code)

Each neighbourhood is always one row in this database. Neighbourhoods should not be duplicated, but if they are, for example because of similar names in the OSM map, the code chooses the smallest one with no childs

#### Scorelogs table
The `ScoreLogs` table consists of 4 columns:
* `Id`: Primary key
* `NeighbourhoodId`: Foreign key pointing to the neighbourhood that the score record belongs to
* `Score`: The score (float, raning from 1 to 10, 10 meaning the worse (red) and 0 meaning the best)
* `Date`: The date of the score record

## ToDo
* Implement the functionality to log out users for certian time of inactivity
* Store settings using a different, more efficient method (possibly another table?)
* Reformat and organize the code, and add more comments

