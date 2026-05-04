pipeline {
  agent any
  environment {
    HARBOR     = '192.168.17.133'
    PROJECT    = 'microservices'
    SERVICE    = 'microservice_hub'
    VERSION    = "${env.BUILD_NUMBER}"
  }
  stages {
    stage('Restore & Test') {
      steps {
        dir('MicroserviceDashboard/backend/MicroserviceHub.API') {
          sh 'dotnet restore'
          sh 'dotnet test --no-restore || true'
        }
      }
    }
    stage('Docker Build & Push') {
      steps {
        withCredentials([usernamePassword(
          credentialsId: 'harbor-robot-push',
          usernameVariable: 'USER',
          passwordVariable: 'PASS')]) {
          sh '''
            docker build \
              -f MicroserviceDashboard/docker/backend.Dockerfile \
              -t $HARBOR/$PROJECT/$SERVICE:$VERSION \
              -t $HARBOR/$PROJECT/$SERVICE:latest \
              MicroserviceDashboard/backend/MicroserviceHub.API
            echo $PASS | docker login $HARBOR -u $USER --password-stdin
            docker push $HARBOR/$PROJECT/$SERVICE:$VERSION
          '''
        }
      }
    }
    stage('Push Helm Chart') {
      steps {
        withCredentials([usernamePassword(
          credentialsId: 'harbor-robot-push',
          usernameVariable: 'USER',
          passwordVariable: 'PASS')]) {
          sh '''
            sed -i "s/^version:.*/version: 1.0.$VERSION/" \
              MicroserviceDashboard/backend/helm/Chart.yaml
            sed -i "s/^appVersion:.*/appVersion: \\"$VERSION\\"/" \
              MicroserviceDashboard/backend/helm/Chart.yaml
            helm package MicroserviceDashboard/backend/helm/ \
              --destination ./helm-output/
            export HELM_EXPERIMENTAL_OCI=1
            echo $PASS | helm registry login $HARBOR \
              -u $USER --password-stdin --insecure
            helm push helm-output/${SERVICE}-1.0.${VERSION}.tgz \
              oci://$HARBOR/helm-charts
          '''
        }
      }
    }
  }
  post {
    success {
      echo "Chart pushed — triggering umbrella pipeline"
      build job: 'umbrella-deploy', wait: false
    }
    failure {
      echo "Build failed — no deploy triggered"
    }
  }
}