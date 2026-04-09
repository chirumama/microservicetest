pipeline {
    agent any

    environment {
        REGISTRY = "192.168.17.133"
        PROJECT = "microservice_hub"
        BACKEND_IMAGE = "${REGISTRY}/${PROJECT}/backend:v1"
        FRONTEND_IMAGE = "${REGISTRY}/${PROJECT}/frontend:v1"
    }

    stages {

        stage('Checkout Code') {
            steps {
                git branch: 'main', url: 'https://github.com/paypoint/MicroserviceDashboard.git'
            }
        }

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
                withCredentials([usernamePassword(credentialsId: 'harbor-creds', usernameVariable: 'USER', passwordVariable: 'PASS')]) {
                    sh 'docker login http://192.168.17.133 -u $USER -p $PASS'
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
                sh 'kubectl apply -f k8s/'
            }
        }
    }
}