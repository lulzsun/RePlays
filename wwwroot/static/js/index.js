var supportedLngs = ["en"];
var resources = {};

(async () => {
  for (var i = 0; i < supportedLngs.length; i++) {
    const response = await fetch(`static/locales/${supportedLngs[i]}.json`);
    const data = await response.json();
    resources[supportedLngs[i]] = { translation: data };
  }

  i18next.init({
    lng: "en",
    fallbackLng: "en",
    debug: true,
    supportedLngs,
    resources
  }, function (err, t) {
    if (err) console.error(err, t);
  });

  window.$t = i18next.t;
  console.log($t('title.sessions'));
  initialize();
})();

window.addEventListener('load', function () {
  // Initialize
  SpatialNavigation.init();

  // Define navigable elements (anchors and elements with "focusable" class).
  SpatialNavigation.add('sidebar', {
    id: 'sidebar',
    selector: '#sidebar .focusable'
  });

  document.addEventListener('sn:focused', function (e) {
    e.target.scrollIntoView({
      behavior: 'smooth',
      block: 'center',
      inline: 'center'
    });
  });

  // Make the *currently existing* navigable elements focusable.
  SpatialNavigation.makeFocusable();

  Gamepad.init();
});
function initialize() {
  if (window.chrome !== undefined) {
    window.chrome.webview.postMessage({ message: "Initialize", data: null });
    return;
  }
  if (window.external.sendMessage !== undefined) {
    window.external.sendMessage(JSON.stringify({ message: "Initialize", data: null }));
    return;
  }
}

// listen for WebView2 messages and attempt to swap in html
// html fragments should be 'hx-swap-oob' for successful swaps
window.chrome.webview.addEventListener("message", (event) => {
  htmx.swap(
    document.createElement('div'),
    event.data.data,
    { swapStyle: 'none' },
    {
      afterSwapCallback: () => {
        console.log(event.data.message, { data: event.data.data });
        SpatialNavigation.makeFocusable();
      }
    });
});
const patchedSend = async function () {
  // Make readonly properties writable
  Object.defineProperty(this, "readyState", { writable: true })
  Object.defineProperty(this, "status", { writable: true })
  Object.defineProperty(this, "statusText", { writable: true })
  Object.defineProperty(this, "response", { writable: true })

  // Set response
  console.log(this.path, { data: JSON.stringify(this.params) });
  window.chrome.webview.postMessage({
    message: this.path,
    data: JSON.stringify(this.params),
    userAgent: window.navigator.userAgent
  });
  this.response = '</>';
  this.readyState = XMLHttpRequest.DONE;
  this.status = 200;
  this.statusText = "OK";

  // We only need load event to trigger a XHR response
  this.dispatchEvent(new ProgressEvent("load"));
};

// If this is inside of a WebView2 component, we need to patch HTMX requests to use
// CoreWebView2's messaging protocol instead of making HTTP requests
if (window.chrome.webview.postMessage !== undefined) {
  document.addEventListener('htmx:beforeSend', (event) => {
    const path = event.detail.requestConfig.path;
    event.detail.xhr.path = path;
    event.detail.xhr.params = event.detail.requestConfig.parameters;
    event.detail.xhr.send = patchedSend;
  });
}