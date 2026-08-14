var supportedLngs = ["en"];
var resources = {};

// Which device the user last acted with, so the focus ring can be shown only when it is doing
// some work. Gamepad.js clears the flag too, since a pad reports through neither of these.
document.addEventListener('pointerdown', function () {
  document.documentElement.classList.add('using-pointer');
}, true);

document.addEventListener('keydown', function () {
  document.documentElement.classList.remove('using-pointer');
}, true);

window.addEventListener('load', async function () {
  for (var i = 0; i < supportedLngs.length; i++) {
    const response = await fetch(`/static/locales/${supportedLngs[i]}.json`);
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
    if (err) return console.error(err, t);

    window.$t = t;
    window.$sn = SpatialNavigation;

    // Initialize
    $sn.init();

    // Define navigable elements (anchors and elements with "focusable" class).
    $sn.add('sidebar', {
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

    $sn.set({
      navigableFilter: function (elem) {
        // An open menu floats over the rest of the page, and spatial navigation only compares
        // geometry, so a control painted underneath it can be the nearest candidate. While a
        // dropdown is open its own entries are the only thing to move between.
        const openDropdown = document.querySelector('details.dropdown[open]');
        if (openDropdown !== null && !openDropdown.contains(elem)) return false;

        // A closed daisyUI dropdown keeps its menu in the layout (visibility: hidden), which the
        // size check in isNavigable() does not catch, so filter those items out explicitly.
        if (typeof elem.checkVisibility !== 'function') return true;
        return elem.checkVisibility({ visibilityProperty: true });
      }
    });

    // Nothing native dismisses an open <details>, so give the keyboard the usual way out; the
    // summary keeps focus, which is where the user was before opening it.
    document.addEventListener('keydown', function (e) {
      if (e.key !== 'Escape') return;
      const openDropdown = document.querySelector('details.dropdown[open]');
      if (openDropdown === null) return;

      openDropdown.open = false;
      openDropdown.querySelector('summary').focus();
      e.stopPropagation();
    });

    // Make the *currently existing* navigable elements focusable.
    $sn.makeFocusable();

    Gamepad.init();

    console.log($t('title.sessions'));
    initialize();
  });
});

// Settings tabs with conditional sections are re-rendered by the server after a save, so that
// the markup always matches the model. Only the generic 'PUT /settings' needs this; the dedicated
// settings routes already respond with the re-rendered tab.
function shouldRefreshSettingsTab(event) {
  const config = event.detail && event.detail.requestConfig;
  if (config === undefined || event.detail.successful !== true) return false;
  return config.verb === 'put' && config.path === '/settings';
}

// The daisyUI menu is only the desktop face of a Dropdown's <select>: picking an entry writes the value
// through and lets the select raise the request, so both faces go down the same htmx path.
function dropdownItemPicked(item) {
  const dropdown = item.closest('.dropdown');
  const select = dropdown.querySelector('select');

  select.value = item.value;
  select.nextElementSibling.innerText = item.innerText;
  htmx.trigger(select, 'change');

  // collapsing the <details> hides the item that was focused, so hand focus back to the button
  dropdown.open = false;
  dropdown.querySelector('summary').focus();
}

const handlePopState = function (e) {
  let url = window.location.pathname;
  if (e !== undefined && e.state !== undefined) url = e.state;
  switch (url) {
    case "/clips":
      showClips(false);
      break;
    case "/sessions":
    default:
      if (url.startsWith("/player/")) {
        playPauseVideo(url.replace('/player/', ''))
      } else {
        showSessions(false);
      }
      break;
  }
}

window.addEventListener('popstate', handlePopState);

function showSessions(e) {
  if (document.getElementById('sessions-nav').checked) {
    return;
  }
  document.getElementById('sessions-nav').checked = true;
  document.getElementById('sidebar').classList.toggle('-translate-x-full');
  playPauseVideo();
  if (e === undefined || e !== false) window.history.pushState('/sessions', '/sessions', '/sessions');
}

function showClips(e) {
  if (document.getElementById('clips-nav').checked) {
    return;
  }
  document.getElementById('clips-nav').checked = true;
  document.getElementById('sidebar').classList.toggle('-translate-x-full');
  playPauseVideo();
  if (e === undefined || e !== false) window.history.pushState('/clips', '/clips', '/clips');
}

function showSettings() {
  document.getElementById('settings-nav').checked = true;
  document.getElementById('settings-dialog').classList.remove('hidden');
  $sn.focus('settingsPage');
}

function initialize() {
  const newDiv = document.createElement('div');

  newDiv.setAttribute('hx-get', '/initialize');
  if (window.location.pathname === '/')
    newDiv.setAttribute('hx-replace-url', '/sessions');
  newDiv.setAttribute('hx-trigger', 'load');

  document.body.appendChild(newDiv);
  htmx.process(newDiv);

  if (window.chrome !== undefined) {
    window.chrome.webview.postMessage("Initialize");
    return;
  }
  //if (window.external.sendMessage !== undefined) {
  //  window.external.sendMessage(JSON.stringify({ message: "Initialize", data: null }));
  //  return;
  //}
}

//const patchedSend = async function () {
//  // Make readonly properties writable
//  Object.defineProperty(this, "readyState", { writable: true })
//  Object.defineProperty(this, "status", { writable: true })
//  Object.defineProperty(this, "statusText", { writable: true })
//  Object.defineProperty(this, "response", { writable: true })

//  // Set response
//  console.log(this.path, { data: JSON.stringify(this.params) });
//  window.chrome.webview.postMessage({
//    message: this.path,
//    data: JSON.stringify(this.params),
//    userAgent: window.navigator.userAgent
//  });
//  this.response = '</>';
//  this.readyState = XMLHttpRequest.DONE;
//  this.status = 200;
//  this.statusText = "OK";

//  // We only need load event to trigger a XHR response
//  this.dispatchEvent(new ProgressEvent("load"));
//};

// If this is inside of a WebView2 component, we need to patch HTMX requests to use
// CoreWebView2's messaging protocol instead of making HTTP requests
//if (window.chrome !== undefined && window.chrome.webview.postMessage !== undefined) {
//  // listen for WebView2 messages and attempt to swap in html
//  // html fragments should be 'hx-swap-oob' for successful swaps
//  window.chrome.webview.addEventListener("message", (event) => {
//    htmx.swap(
//      document.createElement('div'),
//      event.data.data,
//      { swapStyle: 'none' },
//      {
//        afterSwapCallback: () => {
//          console.log(event.data.message, { data: event.data.data });
//          SpatialNavigation.makeFocusable();
//        }
//      });
//  });
//  document.addEventListener('htmx:beforeSend', (event) => {
//    const path = event.detail.requestConfig.path;
//    event.detail.xhr.path = path;
//    event.detail.xhr.params = event.detail.requestConfig.parameters;
//    event.detail.xhr.send = patchedSend;
//  });
//}