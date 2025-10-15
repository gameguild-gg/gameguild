# Building a 2D Platformer Game with Unity

In this assignment, you will create a basic 2D platformer game using Unity. The game will feature a player character that can move, jump, and collect objects while avoiding obstacles. The player will navigate between different scenes using mouse.

::: danger

Do the following steps on your forked repository from the previous week!

:::

Requirements:

## 1. Scene 1 (Menu Scene):

- Create a start screen with a "Start Game" button.
- Include a "Quit Game" button (only functional in a build).
- The "Start Game" button should transition to the Game Scene.

## 2. Scene 2 (Game Scene):

- Create a simple 2D platformer environment with platforms, obstacles, and collectible items (coins, stars, etc.).
- Implement basic player movement (left, right, jump).
- The player must be able to jump between platforms, avoid obstacles, and collect items.
- Add a "Return to Menu" button in the Game Scene to take the player back to the Menu.

## Step-by-Step Guide:

### 1. Setting Up the Project:

- Open Unity targeting your current repo;
- Set up two scenes: one for the Menu (Scene 1) and one for the Game (Scene 2). Place them in a folder named "Scenes". 
- Add both scenes to the File -> Build Settings. Ensure menu is the first.

### 2. Menu Scene:

- Ensure your camera is cartographic. Click the main camera on hiararchy; on the inspector, click projection orthographic;
- Create a UI Canvas. Right-click on the hierarchy -> UI -> canvas.
- (optional) Click the canvas on the hierarchy. On the inspector, change the UI scale mode to Scale with Screen Size. Then, change the Screen Match mode to Match Width or Height.
- Add two buttons. On the hierarchy, right-click on the canvas, UI, then the button:
    - Start Game – Use this button to load the Game Scene when clicked.
    - Quit Game – Exits the game when clicked (optional for testing in the editor, use Application.Quit() in the build).
- Create a folder Scripts on project assets (if not there), then right-click in the scripts folder, Create, then new C# script, name it SceneMenuTransition or anything meaningful to you.
- Create two public functions on the script to execute what you want to do with the buttons. Ex.: 

``` c#
public void ChangeScene(string sceneName)
{
    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
}
```
- Attach your script to the camera or canvas. Click on your script on the project tab, then drag it to the desirable game object.
- Click on your button in the hierarchy. On the inspector, click the OnClick() + button. Drag the game object hosting your script, let's say the Camera, to the "None (object)" slot. On the right side, select the component named SceneMenuTransition. Then select the function you created, ChangeScene. Finally, write the name of the scene in the field below.

### 3. Game Scene:

- Design the platformer environment:
    - Create platforms (use Unity 2D sprites or import assets).
    -Place collectible items (such as stars or coins) across the platforms.
    - Add at least one moving obstacle or hazard (like spikes or enemies).
- Create the player character:
    - Add a simple sprite for the player, similar to how we did last class.
    - Attach a Rigidbody2D and a Collider2D to the player to handle physics and collisions.
    - Instead of changing the transform position we did last time, we will use rigid-body and change the velocity. this will allow the physics engine deal with collisions for us.
    - Script basic movement using arrow keys or WASD keys (horizontal movement and jumping).
``` c#
// cache the rigidbody to avoid calling GetComponent every update
private Rigidbody2D rb;
void Start()
{
    this.rb = GetComponent<Rigidbody2D>();
}

public void Update()
{
    if (Input.GetKey(KeyCode.W))
        rb.velocity = new Vector2(0, 1); // change this
    // ... continue
}
```
- Script collectible items:
    - When the player touches an item, it should disappear and increase the player's score.

### 4. UI in the Game Scene:

- Add a button for "Return to Menu" that takes the player back to the Menu Scene using SceneManager.LoadScene.

Features to Implement:

- Player Movement: The player can move left, right, and jump.
- Collectible Items: Items that disappear when collected, increasing the score.
- Obstacles: Objects or traps that the player must avoid.
- Scene Management: Transitions between Menu and Game scenes using buttons.

### Submission:

Submit the Unity project repo url containing your scenes, scripts, and assets. I will grade by checking your code and the WebGL build. Be sure the project includes:

- Both the Menu Scene and Game Scene.
- Working buttons for navigating between the scenes.
- Player Movement: The player can move left, right, and jump.
- Collectible Items: Items that disappear when collected, increasing the score.
- Obstacles: Objects or traps that the player must avoid.
- Scene Management: Transitions between Menu and Game scenes using buttons.

### Bonus (Optional):

- Add sound effects when collecting items and jumping.
- Add a victory message when all items are collected.
- Add animations to the player (idle, running, jumping).

::: note

NOTE: If you are using other engines rather than Unity, talk to me in class, so I can guide you on how you build targeting WebGL.

:::