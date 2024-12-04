const videoElement = document.querySelector('video');
const timelineElement = document.getElementById("timelineElement");
const seekWindowElement = document.getElementById("seekWindowElement");
const seekBarElement = document.getElementById("seekBarElement");

let currentZoom = 0;
const ZOOMS = [
  100, 110, 125, 150, 175, 200, 250, 300, 400, 500, 1000, 2000, 3000, 4000, 5000, 7500, 10000,
];

var seekDragging = false,
  clipDragging = -1,
  clipDragOffset = 0,
  clipResizeDir = '',
  clipResizeLimit = 0;

const playPauseVideo = function (src) {
  if (!videoElement.paused || src === undefined) {
    if (!videoElement.paused && src === undefined) {
      cleanUp();
    }
    videoElement.pause();
    return;
  }

  videoElement.src = src;
  init();
  document.getElementById('player-nav').checked = true;
}

const init = function () {
  videoElement.load();
  videoElement.play();

  //document.addEventListener('keydown', handleOnKeyDown);
  document.addEventListener('mousedown', handleOnMouseDown);
  //document.addEventListener('mousemove', handleOnMouseMove);
  //document.addEventListener('mouseup', handleOnMouseUp);
  //document.addEventListener('wheel', handleWheelScroll);

  seekWindowElement.previousElementSibling.style.width = `calc(${ZOOMS[currentZoom]}% - 12px`;
  seekWindowElement.style.width = `calc(${ZOOMS[currentZoom]}% - 12px`;
}

const cleanUp = function () {
  //document.removeEventListener('keydown', handleOnKeyDown);
  document.removeEventListener('mousedown', handleOnMouseDown);
  //document.removeEventListener('mousemove', handleOnMouseMove);
  //document.removeEventListener('mouseup', handleOnMouseUp);
  //document.removeEventListener('wheel', handleWheelScroll);

  videoElement.currentTime = 0;
}

const handleVideoPlaying = function () {
  seekBarElement.style.left = `calc(${(videoElement.currentTime / videoElement.duration) * 100}% - 3px)`;
  //targetSeekElement.style.left = seekBarElement.offsetLeft + 6 + 'px';
}

const mouseSeek = function (e) {
  let clickLeft =
    e.clientX + timelineElement.scrollLeft - seekWindowElement.offsetLeft;
  if (clickLeft < 0) clickLeft = 0;
  else if (clickLeft > seekWindowElement.clientWidth)
    clickLeft = seekWindowElement.clientWidth;
  let newCurrentTime =
    (clickLeft / seekWindowElement.clientWidth) * videoElement.duration;
  videoElement.currentTime = newCurrentTime;
  seekBarElement.style.left = `${clickLeft - 3}px`;
}

const handleOnMouseDown = function (e) {
  let element = e.target;
  element = findBookmarkAncestorOrSelf(element);

  if (e.button === 0) { // left click handling
    // seeker handling
    if (element === seekBarElement) {
      seekDragging = true;
    } else if (seekWindowElement.contains(element)) {
      if (e.detail === 1) {
        if (element === seekWindowElement) mouseSeek(e);
      } else if (e.detail === 2) {
        mouseSeek(e);
      }
    }
  }
}

function handleOnMouseUp(e) {
  seekDragging = false;
  clipResizeDir = '';
  if (clipDragging !== -1) {
    let clipsCopy = [...clips];
    clipsCopy[clipDragging].start =
      (clipsRef.current[clipDragging].offsetLeft / seekWindowElement.clientWidth) *
      100;
    clipsCopy[clipDragging].duration =
      (clipsRef.current[clipDragging].offsetWidth / seekWindowElement.clientWidth) *
      100;
    setClips(clipsCopy);
    clipDragging = -1;
  }
}

// Enables clicking on the bookmark icon.
const findBookmarkAncestorOrSelf = function (e) {
  const bookmarkElement = e.closest('.bookmark');
  return bookmarkElement || e;
}