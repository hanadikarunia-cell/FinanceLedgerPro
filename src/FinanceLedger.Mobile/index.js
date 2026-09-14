import { registerRootComponent } from 'expo';
import App from './App';
// Import for side effect: registers the background-fetch TaskManager task at
// module scope so the OS can invoke it even after a cold start.
import './src/offline/backgroundSync';

// registerRootComponent calls AppRegistry.registerComponent('main', () => App);
// It also ensures that whether you load the app in Expo Go or in a native build,
// the environment is set up appropriately.
registerRootComponent(App);
