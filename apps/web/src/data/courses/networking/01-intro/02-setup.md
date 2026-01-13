# Setup

## GIT Setup

Install any GIT client of your choice. I recommend [git-fork](https://git-fork.com/) for beginners, but you can also use the command line if you prefer.

## Repo Setup

Go to [the course repository](https://github.com/gameguild-gg/network). Follow the instructions there.

## IDE Setup

You may want to use any IDE of your choice, but I will be using JetBrains IDEs for this course, so it will be easier to follow along if you do the same. VS Code is also a good choice, but I will not be providing specific instructions for it,besides that, the contextual autocomplete and other features in JetBrains IDEs are generally better.

1. (Optional) [Apply for Jetbrains Student License](https://www.jetbrains.com/academy/student-pack/). A student license gives you free access to all JetBrains IDEs;
2. Install [Jetbrains Toolbox](https://www.jetbrains.com/toolbox-app/);
3. Login to Jetbrains Toolbox using your student account;
   - Once you log in, you will have access to all JetBrains IDEs for free as long as you are a student.
4. Install the following tools via Jetbrains Toolbox, they are free for students and non-commercial use
   - [CLion](https://www.jetbrains.com/clion/). This will be extensively used for assignments involving C/C++ programming.

::: warning "Disable AI Assistance"

Please disable AI assistance features (like GitHub Copilot, ChatGPT plugins, etc.) in any IDE you use for this course. Relying on AI tools can hinder your learning process and may lead to academic integrity issues. Read more about our [Academic Honesty](https://gameguild.gg/academic-honesty/).

On JetBrains IDEs, you can disable:

- AI assistance by going to `Settings/Preferences` > `Plugins` and disabling any AI-related plugins, such as
  - `GitHub Copilot`,
  - `Trae AI`,
  - or any other similar plugins you may have installed.
- In WebStorm, you can also disable AI code completion by going to `Settings/Preferences` > `Editor` > `General` > `Inline Completion` and unchecking the option for AI-based suggestions: such as
  - `Enable local Full Line completion Sugestions`,
  - `Enable automatic completion on typing`
  - `Enable multi-line suggestions`.

:::

## Repository structure

- Each assignment is in its own folder named `assignment-<number>`, e.g., `assignment-1`, `assignment-2`, etc.
- Each assignment folder contains:
  - `README.md`: Instructions for the assignment;
  - `docker-compose.yml`: Docker Compose file to set up the required environment;
  - Other files and folders as needed for the assignment, refer to the `README.md` in each assignment folder for details.
- `.github/workflows/`: Contains GitHub Actions workflows for automated testing of assignments. **Do not modify these files**.
- `.gitignore`: Specifies files and folders to be ignored by Git. You may modify this file if needed.
- `scripts/`: Contains helper scripts for running tests and other tasks. Do not modify these files.
- `reports/`: Will contain any generated files or outputs from assignments. Do not modify these files.

## Testing Assignments

- Each assignment includes automated tests that you can run locally using Docker.
- You may want to check the tab "Actions" on GitHub to see the results of automated tests run on your submissions.
- You may want to use `act` to run GitHub Actions workflows locally. Refer to [act documentation](https://github.com/nektos/act).
