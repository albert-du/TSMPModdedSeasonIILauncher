namespace TerminalView
// Signature File

//
// Terminal Interface control for Avalonia.FuncUI (https://github.com/AvaloniaCommunity/Avalonia.FuncUI)
// Supports all 16 System.ConsoleColor colors
//

  /// Data source for terminal control
  type TerminalSource

  /// Module functions for manipulating TerminalSource
  module TerminalSource =
    /// A new empty Terminal Source
    val empty : TerminalSource

    /// Appends Text to Terminal Source
    val write : text:string -> source:TerminalSource -> TerminalSource
    
    /// Appends Text to Terminal Source with a color
    val writeColor : text:string -> color:System.ConsoleColor option -> source:TerminalSource -> TerminalSource

    /// Appends Text to Terminal Source, adds new line 
    val writeLine : text:string -> source:TerminalSource -> TerminalSource

    /// Appends Text to Terminal Source, adds new line 
    val writeLineColor : text:string -> color:System.ConsoleColor option -> source:TerminalSource -> TerminalSource

    /// Handles a key press, required for non read only 
    val handleTextInput : args:Avalonia.Input.TextInputEventArgs -> source:TerminalSource -> TerminalSource

    /// Handles a key press, required for non read only, returns a TerminalSource and a string option for entered text
    val handleKeyInput : args:Avalonia.Input.KeyEventArgs -> source:TerminalSource -> (TerminalSource * string option)

  [<AutoOpen>]
  module Terminal =
    type TerminalAttr

    /// Creates a new Terminal Control 
    val create : attributes:TerminalAttr list -> Avalonia.FuncUI.Types.IView

    /// Attribute constructors for Terminal Control
    [<AbstractClass; Sealed>]
    type Terminal =
        /// Specify a function taking a System.ConsoleColor option to a hex (#XXXXXX) OR another Avalonia.Media.Color.Parse() Compatible string
        /// A default color converter is available provided this is not specified 
        static member colorConverter : func:(System.ConsoleColor -> string) -> TerminalAttr

        /// true to allow for user typing, false for a read only terminal, defaults to false (read-only)
        static member readEnabled : readEnabled:bool -> TerminalAttr

        /// The TerminalSource to display
        static member source : terminalSource:TerminalSource -> TerminalAttr

        /// The default foreground Color
        static member foreground : brush:Avalonia.Media.IBrush -> TerminalAttr
        
        /// The default foreground Color
        static member foreground : brush:string -> TerminalAttr

        /// The background color
        static member background : brush:Avalonia.Media.IBrush -> TerminalAttr

        /// The background color
        static member background : brush:string -> TerminalAttr

        /// fontsize
        static member fontSize : double -> TerminalAttr