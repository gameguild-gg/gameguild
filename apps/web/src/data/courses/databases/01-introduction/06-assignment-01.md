# Introductory assignment

## Install docker

1. Install [Docker Desktop](https://www.docker.com/products/docker-desktop/) on your machine;
2. Make sure Docker is properly installed by running the following command in your terminal/command prompt:

```bash
docker --version
```

## (Optional) Node.js Setup Instructions

- Install [`node.js`](https://nodejs.org/en/download/) LTS version on your machine, for better debugging experience with WebStorm IDE.
- Make sure it is properly installed by running the following commands in your terminal/command prompt:

```bash
node -v
npm -v
```

## Postgres and Adminer setup

1. Read the entire README.md on the root of the repository;
2. Add me (tolstenko) or the instructor's username as a collaborator to your repository on GitHub;
3. Clone your forked repository to your local machine;
4. On WebStorm (or any JetBrains IDE of your choice), open the cloned repository;
5. Navigate to this very folder on the terminal/command prompt.
   - On Jetbrains IDEs, you can open the terminal via `View` > `Tool Windows` > `Terminal`.
6. On the terminal, write `cd assignment-01` then enter;
7. Run `docker-compose up -d postgres adminer` to start a PostgreSQL database and Adminer web interface;
8. Open your web browser and navigate to `http://localhost:8080` to access Adminer;
9. In Adminer, use the following credentials to log in to the PostgreSQL
   - System: PostgreSQL
   - Server: postgres
   - Username: postgres
   - Password: postgres
   - Database: postgres
10. Once logged in, you should see the PostgreSQL database interface. You can now interact with the database using SQL queries which you will learn more about in the upcoming assignments.
11. click on the `SQL command`, then Execute the following query to verify the connection `SELECT version();`
12. Take a screenshot of the Adminer interface showing the successful connection to the PostgreSQL database. Save this screenshot as `adminer_connection.png` in this `assignment-01` folder. The filename should be exactly it.
    - On Windows, you can use the `Snipping Tool` or `Snip & Sketch` to take screenshots, then save it as `adminer_connection.png` in this folder.
    - On MacOS, you can use `Shift + Command + 5` to capture a selected portion of the screen, the files will be saved to your desktop by default, rename it to `adminer_connection.png` and move it to this folder.
    - On Linux, you can use `gnome-screenshot` or `Shutter` to take screenshots. (you use linux, you probably know how to take screenshots already 😀).
13. Add, Commit and push your changes to your forked repository on GitHub.
14. Remember to stop the Docker containers when you are done by running `docker-compose stop` in the terminal. (docker-compose down` will also remove any data stored in the database).

## DataGrip (Optional)

1. Use DataGrip IDE to open this repository folder;
2. When PostgreSQL and Adminer are running, add a new data source in DataGrip:
   - Click on the `+` icon in the Database tool window;
   - Select `Data Source` > `PostgreSQL`;
   - Fill in the connection details:
     - Host: `localhost`
     - Port: `5432`
     - User: `postgres`
     - Password: `postgres`
     - Database: `postgres`
   - Test the connection to ensure everything is set up correctly;
   - You may receive a prompt to download the PostgreSQL JDBC driver, accept it, wait for the download to complete.
3. When the connection is successful, test it by running the query `SELECT version();` in DataGrip.
   - Clic the `+` and then `New Query Console` to open a new SQL editor.
   - Write the query `SELECT version();` and run it.
4. Take a screenshot of DataGrip showing the successful connection and the result of the query.

## Enable GitHub Actions

1. On your forked repository on GitHub, navigate to the `Actions` tab;
2. You should see a prompt to enable GitHub Actions for this repository. Click on the `Enable` button;
   - This will activate the automated testing workflows for your assignments.

## Allow github actions to commit to your repository

1. On your forked repository on GitHub, navigate to the `Settings` tab;
2. On the left sidebar, click on `Actions` > `General`;
3. Scroll down to the `Workflow permissions` section;
4. Select the option `Read and write permissions`;
5. Click on the `Save` button to apply the changes.

## Deliverables

On canvas submit the following:

1. The link of your forked repository on GitHub;
2. I will verify that:
   - You have added me (tolstenko) or the instructor's username as a collaborator to your repository on GitHub;
   - You have enabled GitHub Actions for your repository;
   - You have allowed GitHub Actions to commit to your repository;
   - You have committed and pushed the screenshot `adminer_connection.png` showing the successful connection to PostgreSQL via Adminer;

::: tip

Before submitting, double-check the tests are passing on the `Actions` tab of your repository on GitHub.

:::
