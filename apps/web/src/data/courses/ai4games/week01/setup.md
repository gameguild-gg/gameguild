# Setup the repos

<details>
<summary>Teacher notes</summary>
- Day 1: 
    - Teacher Introduction; 
    - Course Overview; 
    - Expectations; 
    - FERPA Waiver consent form for using github; 
    - Form for receiving feedback about their expectations and topics;
    - Setup Repos;
- Day 2: 
    - Intro to game AI;
    - Setup Repos;
    - Ensure everyone is ready for the class. Check mobagen and ai4games repos.
</details>

1. Read about Privacy and FERPA compliance [here](/ferpa-waiver)
2. `Testable AI` assignments for in class coding assignments. [repo](https://github.com/gameguild-gg/ai4games)
3. `MoBaGEn`, for interactive assignments. [repo](https://github.com/gameguild-gg/mobagen). Please leave a star there to help me gain visibility! Star this [website repo](https://github.com/gameguild-gg/gameguild) too!
4. Install `CLion` (has `CMake` embedded);
5. Install git and add the binaries to your `PATH`;
6. Those repositories are updated constantly. Pay attention to syncing your repo frequently.

## Setup Windows machines to C++ development

<iframe width="560" height="315" src="https://www.youtube.com/embed/mQGqwTuuq9Q?si=IM6pv91MO-k7Vuo7" title="YouTube video player" frameborder="0" allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share" referrerpolicy="strict-origin-when-cross-origin" allowfullscreen></iframe>

## Types of coding assignments

There will be two types of coding assignments:

1. **Formal**: Automatic grading system with automated tests. Some tests might not be fully working for you, talk with me if it doesnt work for you. Or just submit the code on canvas and I will grade it manually. Those should solved using C++; It is done following [this repo](https://github.com/gameguild-gg/ai4games);
2. **Interactive**: For the interactive assignments you can choose whatever Game Engine you like, but I recommend you to use the framework I created for you: [MoBaGEn](https://github.com/gameguild-gg/mobagen). If you use a Game Engine or custom solution for that, you will have to create all debug interfaces to showcase and debug AI which includes, but it is not limited to:

   - Draw vectors to show forces applied by the AI;
   - Menus to change AI parameters;

::: danger

There will be some forks done already from previous students. **Under no circunstaces**, you should check their solutions. We already live in a time of publicly available **LLMs** with auto complete, so you abusing ethiter **AI** usage or stealing work of other students beats the purpose of this class and hurts your learning curve. This course is crafted for you not need to use these tools.

If I detect an abuse of this, **I am obligated to report the student to the college due to misconduct**.

:::

## Code assignments

::: warning

Given the common approach for this class is to produce content to be potentially used as publicly available portfolio piece, you must read [this](https://www2.ed.gov/policy/gen/guid/fpco/ferpa/index.html) and sign the [FERPA waiver consent form](./ferpa). If you are willing to keep your content FERPA compliant, you will have to do some extra steps to keep your code private, you should use a private repo on github or any git provider like, talk to me and I will accomodate your case. Keep in mind you can keep your privacy even in a public environment by just creating a new github account with a random username unrelated to your real identity.

:::

1. Create an account on [github](https://github.com) or any `git` hosting on your preference;
2. Create the repos:

   1. If you want to make it count as part of your portfolio, fork the repo follow [this](https://docs.github.com/en/get-started/quickstart/fork-a-repo);
   2. If you want to keep it private or be FERPA compliant, duplicate the repo following [this](https://docs.github.com/en/repositories/creating-and-managing-repositories/duplicating-a-repository).

3. Add my user to your repo to it with `read` role. My userid is [tolstenko](https://github.com/tolstenko) (or your current instructor) on github (pls follow me on github!), for other options, talk with me in class. Follow [this](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/managing-teams-and-people-with-access-to-your-repository);
4. Send me a message on canvas with the link to your repo, or talk with me in class;

::: note "Private repo"

**GitHub doesnt allow private fork of a public repo**. If you want to receive my updates into your private repo you can do in 2 different ways:

1. Create an empty repo. Clone it to your machine. In your GitKraken or any other git tool, add my repo as another origin. Merge from from mine to yours with flag `--allow-unrelated-histories` (research about it on google or ask any LLMs like Claude).
2. Create an empty repo and clone it to your machine. From time to time, download the files from my repo and replace the ones I might have updated/fixed. But I recommend the first option

:::

## Recordings

In all interactive assignments, you will have to record a 5 minute video explaining your code. I recommend you to use [OBS](https://obsproject.com/), or use the canvas embeded tool, or any software you prefer to record your screen while you explain your code. The reason behind that is to train you to speak about your code and to explain your thought process. This is a relevant thing for your future life as programmer.

## Development tools

I will be using `CMake` for the classes, but you can use whatever you want.

In this class, I am going to use `CLion` as the `IDE`, because it has nice support for `CMake` and automated tests.

- Download it [here](https://www.jetbrains.com/clion/).
- If you are a student, you can get a free license [here](https://www.jetbrains.com/community/education/#students).

If you want to use `Visual Studio`, be assured that you have the `C++ Desktop Development` workload installed, more info [this](https://docs.microsoft.com/en-us/cpp/build/vscpp-step-0-installation?view=msvc-160). And then go to `Individual Components` and install `CMake Tools for Windows`.

::: note

If you use `Visual Studio`, you won't be able to use the automated testing system that comes with the assignments.

:::

[OPINION]: If you want to use a lightweight environment, don't use VS Code for C++ development. Period. It is not a good IDE for that. It is preferred to code via sublime, notepad, vim, or any other text editor and then compile your code via terminal, and debug via gdb, than using VS Code for C++ development.

### Opening the Repos

1. Fork and clone the repos. [Make it private if you can](https://docs.github.com/en/repositories/managing-your-repositorys-settings-and-features/managing-repository-settings/setting-repository-visibility#changing-a-repositorys-visibility);
2. Open `CLion` or yor preferred `IDE` with `CMake`;
3. Open the `CMakeLists.txt` as project from the root of the repo;
4. Wait for the setup to finish (it will download the dependencies automatically, such as `SDL`, `doctest` and others );

For the interactive assignments, use this [repo](https://github.com/gameguild-gg/mobagen) and the assignments are located in the `examples` folder.

For the algorithmic assignments, use this [repo](https://github.com/gameguild-gg/ai4games). I created some automated tests to help you debug your code and ensure 100% of correctness. To run them, follow the steps (only available though `CLion` or terminal, not `Visual Studio`):

1. Go to the executable drop down selection (top right, near the green `run` or `debug` button) and select the assignment you want to run. It will be something like `XXX` where `XXX` is the name of the assignment;
2. If you want to test your assignment against the automated inputs/outputs, select the `XXX-test` build target. Here you should use the `build` button, not the `run` or `debug` button. It will run the tests and show the results in the `Console` tab;
3. I use a bare minimum memory leak detector hand made, use it to help you. If it is getting in your way, do not import it in your code.