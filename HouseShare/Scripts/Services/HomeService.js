houseShare.factory("HomeService", ['$http', '$q',  function ($http, $q) {
    return {
        /* Share Entities */
        getShareEntities:function() {
            var deferred = $q.defer();
            $http.get('Home/LoadEntities?_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        saveShareEntity:function(model) {
            var deferred = $q.defer();
            $http.post("Home/SaveShareEntity", model).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        saveTx:function(model) {
            var deferred = $q.defer();
            $http.post("Home/SaveTx", model).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        loadBalances:function() {
            var deferred = $q.defer();
            $http.get('Home/LoadBalances?_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        loadMonths: function () {
            var deferred = $q.defer();
            $http.get('Home/LoadMonths?_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        loadMonth: function (mnth,yr) {
            var deferred = $q.defer();
            $http.get('Home/LoadMonth?m=' + mnth + '&yr=' + yr + '&_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        loadShares: function (date) {
            var deferred = $q.defer();
            $http.get('Home/LoadShares?dte=' + date +'&_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        },
        recalcAll: function () {
            var deferred = $q.defer();
            $http.get('Home/recalcAll?_=' + new Date().getTime()).success(deferred.resolve).error(deferred.reject);
            return deferred.promise;
        }

    }
}]);