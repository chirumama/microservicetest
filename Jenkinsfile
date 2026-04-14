pipeline {
    agent any

    environment {
        HARBOR_REGISTRY = "192.168.17.133"
        BACKEND_IMAGE   = "${HARBOR_REGISTRY}/microservice_hub/backend:${BUILD_NUMBER}"
        FRONTEND_IMAGE  = "${HARBOR_REGISTRY}/microservice_hub/frontend:${BUILD_NUMBER}"
    }

    stages {

        stage('Build Backend Image') {
            steps {
                sh 'docker build -t $BACKEND_IMAGE -f docker/backend.Dockerfile .'
            }
        }

        stage('Build Frontend Image') {
            steps {
                sh 'docker build -t $FRONTEND_IMAGE -f docker/frontend.Dockerfile .'
            }
        }

        stage('Login to Harbor') {
            steps {
                withCredentials([usernamePassword(
                    credentialsId: 'harbor-creds',
                    usernameVariable: 'USER',
                    passwordVariable: 'PASS'
                )]) {
                    sh 'echo $PASS | docker login http://$HARBOR_REGISTRY -u $USER --password-stdin'
                }
            }
        }

        stage('Push Images') {
            steps {
                sh 'docker push $BACKEND_IMAGE'
                sh 'docker push $FRONTEND_IMAGE'
            }
        }

        stage('Deploy to Kubernetes') {
            steps {
                withCredentials([file(credentialsId: 'k8s-kubeconfig', variable: 'KUBECONFIG')]) {
                    sh '''
                        export KUBECONFIG=$KUBECONFIG
                        sed -i "s|BACKEND_IMAGE_PLACEHOLDER|$BACKEND_IMAGE|g" k8s/backend-deployment.yaml
                        sed -i "s|FRONTEND_IMAGE_PLACEHOLDER|$FRONTEND_IMAGE|g" k8s/frontend-deployment.yaml
                        kubectl apply -f k8s/
                    '''
                }
            }
        }
    }

    post {
        failure {
            echo 'Pipeline failed!'
        }
        success {
            echo 'Deployed successfully!'
        }
    }
}