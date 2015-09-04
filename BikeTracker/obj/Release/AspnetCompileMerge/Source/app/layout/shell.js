(function () { 
    'use strict';
    
    var controllerId = 'shell';
    angular.module('app').controller(controllerId,
        ['$rootScope', '$http', 'common', 'config', shell]);

    function shell($rootScope, $http, common, config) {
        var vm = this;
        var logSuccess = common.logger.getLogFn(controllerId, 'success');
        var events = config.events;
        vm.busyMessage = 'Please wait ...';
        vm.isBusy = true;
        vm.spinnerOptions = {
            radius: 40,
            lines: 7,
            length: 0,
            width: 30,
            speed: 1.7,
            corners: 1.0,
            trail: 100,
            color: '#F58A00'
        };


        activate();

        function activate() {
            logSuccess('Bike tracker loaded!', null, true);
            common.activateController([], controllerId);
        }

        function toggleSpinner(on) { vm.isBusy = on; }

        $rootScope.$on('$routeChangeStart',
            function (event, next, current) { toggleSpinner(true); }
        );
        
        $rootScope.$on(events.controllerActivateSuccess,
            function (data) { toggleSpinner(false); }
        );

        $rootScope.$on(events.spinnerToggle,
            function (data) { toggleSpinner(data.show); }
        );

        $rootScope.$on("fb_statusChange", function (event, args) {
            $rootScope.fb_status = args.status;
            $rootScope.$apply();
        });

        $rootScope.$on("fb_login_succeeded", function (event, args) {
            vm.firstName = args;
            console.log("fb_login_succeeded");
        });
        

        $rootScope.$on("fb_login_failed", function () {
            console.log("fb_login_failed");
        });

        $rootScope.$on("fb_logout_succeded", function () {
            console.log("fb_logout_succeded");
            $rootScope.id = "";
            vm.firstName = null;            
        });
        $rootScope.$on("fb_logout_failed", function () {
            console.log("fb_logout_failed!");
        });

    };
})();