const Gamepad = {
  buttons: {},
  axes: {},
  heldDirection: {
    up: 0,
    down: 0,
    left: 0,
    right: 0
  },

  init: function () {
    navigator.getGamepads();

    window.addEventListener("gamepadconnected", (e) => {
      this.connectGamepad(e.gamepad);
    });

    window.addEventListener("gamepaddisconnected", (e) => {
      this.disconnectGamepad(e.gamepad);
    });

    setInterval(() => {
      this.pollGamepadState();
    }, 50);
  },

  connectGamepad: function (gamepad) {
    console.log(`Gamepad connected: ${gamepad.id}, mapping: ${gamepad.mapping}`);
    this.buttons = {};
    this.axes = {};
  },

  disconnectGamepad: function (gamepad) {
    console.log(`Gamepad disconnected: ${gamepad.id}`);
  },

  pollGamepadState: function () {
    const gamepads = navigator.getGamepads();
    gamepads.forEach((gamepad) => {
      if (gamepad && gamepad.mapping === "standard") {
        gamepad.buttons.forEach((button, index) => {
          if (button.pressed) {
            this.buttonPressed(index);
          } else {
            this.buttonUnpressed(index);
          }
        });

        gamepad.axes.forEach((axis, index) => {
          this.axes[index] = axis;
          this.axisChanged(index, axis);
        });
      }
    });
  },

  buttonPressed: function (buttonIndex) {
    if (this.buttons[buttonIndex] === true) {
      switch (buttonIndex) {
        case 12:
          this.heldDirection.up++;
          if (this.heldDirection.up > 15) {
            SpatialNavigation.move('up');
            this.heldDirection.up -= 2;
          }
          break;
        case 13:
          this.heldDirection.down++;
          if (this.heldDirection.down > 15) {
            SpatialNavigation.move('down');
            this.heldDirection.down -= 2;
          }
          break;
        case 14:
          this.heldDirection.left++;
          if (this.heldDirection.left > 15) {
            SpatialNavigation.move('left');
            this.heldDirection.left -= 2;
          }
          break;
        case 15:
          this.heldDirection.right++;
          if (this.heldDirection.right > 15) {
            SpatialNavigation.move('right');
            this.heldDirection.right -= 2;
          }
          break;  
      }
      return;
    }
    this.buttons[buttonIndex] = true;
    if (!document.hasFocus() || document.activeElement === document.body) SpatialNavigation.focus();

    switch (buttonIndex) {
      case 12:
        console.log(`Button DPAD UP pressed`);
        SpatialNavigation.move('up');
        break;
      case 13:
        console.log(`Button DPAD DOWN pressed`);
        SpatialNavigation.move('down');
        break;
      case 14:
        console.log(`Button DPAD LEFT pressed`);
        SpatialNavigation.move('left');
        break;
      case 15:
        console.log(`Button DPAD RIGHT pressed`);
        SpatialNavigation.move('right');
        break;
      default:
        console.log(`Button ${buttonIndex} pressed`);
        break;
    }
  },

  buttonUnpressed: function (buttonIndex) {
    if (this.buttons[buttonIndex] === false) return;
    this.buttons[buttonIndex] = false;

    switch (buttonIndex) {
      case 12:
        console.log(`Button DPAD UP released`);
        this.heldDirection.up = 0;
        break;
      case 13:
        console.log(`Button DPAD DOWN released`);
        this.heldDirection.down = 0;
        break;
      case 14:
        console.log(`Button DPAD LEFT released`);
        this.heldDirection.left = 0;
        break;
      case 15:
        console.log(`Button DPAD RIGHT released`);
        this.heldDirection.right = 0;
        break;
      default:
        console.log(`Button ${buttonIndex} released`);
        break;
    }
  },

  axisChanged: function (axisIndex, value) {
    let isDeadzone = value === 0 || Math.abs(value).toString().startsWith("0.000");
		switch (axisIndex) {
      case 0:
        if (!isDeadzone) {
          console.log(`Left stick X-axis: ${value}`);
          if (value <= -0.5) SpatialNavigation.move('left');
          if (value >= 0.5) SpatialNavigation.move('right');
        }
        break;
      case 1:
        if (!isDeadzone) {
          console.log(`Left stick Y-axis: ${value}`);
          if (value <= -0.5) SpatialNavigation.move('up');
          if (value >= 0.5) SpatialNavigation.move('down');
        }
        break;
      case 2:
        if (!isDeadzone) {
          console.log(`Right stick X-axis: ${value}`);
        }
        break;
      case 3:
        if (!isDeadzone) {
          console.log(`Right stick Y-axis: ${value}`);
        }
        break;
      default:
        if (!isDeadzone) {
          console.log(`Axis ${axisIndex}: ${value}`);
        }
        break;
		}
	},
};

window.Gamepad = Gamepad;