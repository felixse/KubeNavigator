import { ITheme, Terminal } from "@xterm/xterm";
import { FitAddon } from '@xterm/addon-fit';
import { WebglAddon } from "@xterm/addon-webgl";
import { match } from "ts-pattern";

type OutgoingMessage = InputReceived | TerminalSizeChanged;
type IncomingMessage = InitializeTerminal | OutputReceived | SearchTextChanged | ClearRequested | ThemeChanged;

interface InputReceived {
  type: 'InputReceived';
  data: string;
}

interface TerminalSizeChanged {
  type: 'TerminalSizeChanged';
  cols: number;
  rows: number;
}

interface OutputReceived {
  type: 'OutputReceived';
  data: string;
}

interface SearchTextChanged {
  type: 'SearchTextChanged';
  text: string;
}

interface ClearRequested {
  type: 'ClearRequested';
}

interface InitializeTerminal {
  type: 'InitializeTerminal';
  theme: 'dark' | 'light';
  readOnly: boolean;
}

interface ThemeChanged {
  type: 'ThemeChanged';
  theme: 'dark' | 'light';
}

interface ChromeWindow extends Window {
  chrome: Chrome;
}

interface Chrome {
  webview: WebView;
}

interface WebView {
  postMessage: (message: OutgoingMessage) => void;
  addEventListener: (event: string, listener: (message: { data: IncomingMessage }) => void) => void;
}

declare let window: ChromeWindow;

// vscode default colors:
// https://github.com/microsoft/vscode/blob/567779ca5154518823ac35f2cdb0efa36fe1cba7/src/vs/workbench/contrib/terminal/common/terminalColorRegistry.ts#L184
let darkTheme: ITheme = {
  black: '#000000',
  red: '#cd3131',
  green: '#0DBC79',
  yellow: '#e5e510',
  blue: '#2472c8',
  magenta: '#bc3fbc',
  cyan: '#11a8cd',
  white: '#e5e5e5',
  brightBlack: '#666666',
  brightRed: '#f14c4c',
  brightGreen: '#23d18b',
  brightYellow: '#f5f543',
  brightBlue: '#3b8eea',
  brightMagenta: '#d670d6',
  brightCyan: '#29b8db',
  brightWhite: '#e5e5e5',
  foreground: '#ffffff',
  background: '#202020',
  selectionBackground: '#264f78',
  cursor: '#ffffff',
};

let lightTheme: ITheme = {
  black: '#000000',
  red: '#cd3131',
  green: '#107C10',
  yellow: '#949800',
  blue: '#0451a5',
  magenta: '#bc05bc',
  cyan: '#0598bc',
  white: '#555555',
  brightBlack: '#666666',
  brightRed: '#cd3131',
  brightGreen: '#14CE14',
  brightYellow: '#b5ba00',
  brightBlue: '#0451a5',
  brightMagenta: '#bc05bc',
  brightCyan: '#0598bc',
  brightWhite: '#a5a5a5',
  foreground: '#000000',
  background: '#f3f3f3',
  selectionBackground: '#add6ff',
  cursor: '#000000',
};

let term = new Terminal();
const fitAddon = new FitAddon();
const webGlAddon = new WebglAddon();
term.loadAddon(fitAddon);
term.loadAddon(webGlAddon);
term.onData((data) => {
  let message: InputReceived = { type: 'InputReceived', data: data };
  window.chrome.webview.postMessage(message);
});
term.onResize((size) => {
  let message: TerminalSizeChanged = { type: 'TerminalSizeChanged', cols: size.cols, rows: size.rows };
  window.chrome.webview.postMessage(message);
});


term.options.fontFamily = 'Cascadia Code';
term.options.cursorBlink = true;
term.options.cursorStyle = 'bar';

window.chrome.webview.addEventListener('message', (message: { data: IncomingMessage }) => {
  match(message.data)
    .with({ type: 'InitializeTerminal' }, ({ theme, readOnly }) => {
      if (theme === 'dark') {
        term.options.theme = darkTheme;
      } else {
        term.options.theme = lightTheme;
      }

      term.options.disableStdin = readOnly;

      let terminalElement = document.getElementById('terminal');
      if (terminalElement != null) {
        term.open(terminalElement);
        fitAddon.fit();
        term.focus();
      }
    })
    .with({ type: 'OutputReceived' }, ({ data }) => {
      term.write(data);
    })
    .with({ type: 'ClearRequested' }, () => {
      term.clear();
    })
    .with({ type: 'SearchTextChanged' }, ({ text }) => {
      console.log('search text changed: ' + text);
      //term.findNext(text);
    })
    .with({ type: 'ThemeChanged' }, ({ theme }) => {
      term.options.theme = theme === 'dark' ? darkTheme : lightTheme;
    });
});


let resizeTimeout: number;
window.onresize = () => {
  clearTimeout(resizeTimeout);
  resizeTimeout = setTimeout(() => fitAddon.fit(), 500);
};
