(function () {
    'use strict';

    var controllerId = 'loginCtrl';

    angular.module('app').controller(controllerId, ['facebookSvc', 'common', login]);

    function login(facebookSvc, common) {

        var vm = this;
        var logSuccess = common.logger.getLogFn(controllerId, 'success');

        vm.getLoginStatus = facebookSvc.getLoginStatus;
        vm.login = facebookSvc.login;
        vm.logout = facebookSvc.logout;
        vm.unsubscribe = facebookSvc.unsubscribe;
        vm.fbConnected = facebookSvc.fbConnected;
        vm.authenticated = facebookSvc.authenticated;

        activate();

        function activate() {
            logSuccess('Login loaded!', null, true);
            common.activateController([], controllerId);
        }
    }
})();