# QuizGame architecture

Although QuizGame is minimalistic, it has a realistic architecture that reflects best practices for larger projects. The networking, game logic, user interaction, and display functionality are kept loosely coupled, making it easier to modify, replace, or reuse individual components of the app, among other benefits. 

## Separation of concerns

The P2PHelper component is completely agnostic about the messages that it sends, but to keep it decoupled from other code, the rest of the app accesses it only through an adapter (The *Communicator types) that exposes a game-oriented interface. 

The app works with any valid implementations of this interface, and there are mock versions of the adapters that take advantage of this fact. These mock adapters communicate directly, bypassing the network to enable isolation testing of the UI. (You can enable test mode by setting the conditional compilation symbol LOCALTESTMODEON in the project's build properties.) 

## MVVM

The app uses the Model-View-ViewModel (MVVM) architecture to integrate the various components. The **HostViewModel** and **ClientViewModel** classes access one another through the **IHostCommunicator** and **IClientCommunicator** interfaces (implemented by the adapter described above). The HostViewModel also accesses the game logic (the model layer) through the **IGame** interface. Both view-models expose properties and commands for their respective UIs (the view layer) to bind to. 

## Current state

This app is a work in progress made available for early review. It started out as a Windows 8.1 Universal app and was converted to a Windows 10 UWP app. Forthcoming updates will take increasing advantage of UWP platform features to demonstrate best practices for UWP app development. Feel free to send us your feedback on any aspect of this sample. 

## Next steps

This sample is intentionally simplistic within the architectural requirements just described. The goal is not to build a complete, full-featured, well-polished trivia game app, but rather to demonstrate some basic possibilities that can go in several different directions. The decoupled architecture makes it easier to borrow pieces, integrate other code, or use as a template for a different app. Let us know if you have any ideas on where it might go next!

